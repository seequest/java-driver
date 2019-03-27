/*
 * Copyright DataStax, Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

package com.microsoft.azure.cosmosdb.cassandra;

import static com.google.common.base.Preconditions.checkState;

import io.netty.bootstrap.Bootstrap;
import io.netty.bootstrap.ServerBootstrap;
import io.netty.buffer.Unpooled;
import io.netty.channel.Channel;
import io.netty.channel.ChannelFuture;
import io.netty.channel.ChannelFutureListener;
import io.netty.channel.ChannelHandlerContext;
import io.netty.channel.ChannelInboundHandlerAdapter;
import io.netty.channel.ChannelInitializer;
import io.netty.channel.ChannelOption;
import io.netty.channel.EventLoopGroup;
import io.netty.channel.nio.NioEventLoopGroup;
import io.netty.channel.socket.SocketChannel;
import io.netty.channel.socket.nio.NioServerSocketChannel;
import io.netty.handler.logging.LogLevel;
import io.netty.handler.logging.LoggingHandler;
import io.netty.handler.ssl.SslContext;
import io.netty.handler.ssl.SslContextBuilder;
import io.netty.handler.ssl.SslHandler;
import io.netty.util.concurrent.Future;
import io.netty.util.concurrent.GlobalEventExecutor;
import io.netty.util.concurrent.Promise;
import io.netty.util.concurrent.PromiseCombiner;
import java.net.SocketAddress;
import java.util.concurrent.atomic.AtomicBoolean;
import javax.net.ssl.SSLException;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

class GatewayProxy implements AutoCloseable {

  private static final Logger logger = LoggerFactory.getLogger(GatewayProxy.class);
  private final EventLoopGroup boss = new NioEventLoopGroup(1);
  private final EventLoopGroup workers = new NioEventLoopGroup();
  private final SslContext sslContext;
  private final AtomicBoolean closed = new AtomicBoolean();

  GatewayProxy() throws SSLException {
    this.sslContext = SslContextBuilder.forClient().build();
  }

  @Override
  public void close() {

    if (this.closed.compareAndSet(true, true)) {
      return;
    }

    GlobalEventExecutor.INSTANCE
        .next()
        .execute(
            () -> {
              PromiseCombiner combiner = new PromiseCombiner(GlobalEventExecutor.INSTANCE);
              Promise<Void> promise = GlobalEventExecutor.INSTANCE.newPromise();
              combiner.add(this.workers.shutdownGracefully());
              combiner.add(this.boss.shutdownGracefully());
              combiner.finish(promise);

              promise.addListener(
                  future -> {
                    if (future.isSuccess()) {
                      logger.debug("shutdown");
                      return;
                    }
                    logger.error("shutdown failed due to {}", future.cause().toString());
                  });
            });
  }

  Future start(SocketAddress[] proxyAddresses, SocketAddress serviceAddress) {

    checkState(
        !this.closed.get(),
        "cannot start listener on %s because gateway proxy is closed",
        (Object) proxyAddresses);

    final ServerBootstrap bootstrap =
        new ServerBootstrap()
            .channel(NioServerSocketChannel.class)
            .group(this.boss, this.workers)
            .handler(new LoggingHandler(LogLevel.INFO))
            .childHandler(
                new ChannelInitializer<SocketChannel>() {
                  @Override
                  protected void initChannel(SocketChannel sourceChannel) throws Exception {
                    sourceChannel
                        .pipeline()
                        .addLast(
                            new LoggingHandler(LogLevel.INFO),
                            new SourceHandler(serviceAddress, GatewayProxy.this.sslContext));
                  }
                })
            .childOption(ChannelOption.AUTO_READ, false);

    final PromiseCombiner combiner = new PromiseCombiner(GlobalEventExecutor.INSTANCE);
    Promise<Void> promise = GlobalEventExecutor.INSTANCE.newPromise();

    GlobalEventExecutor.INSTANCE
        .next()
        .execute(
            () -> {
              for (SocketAddress proxyAddress : proxyAddresses) {
                combiner.add(
                    bootstrap
                        .bind(proxyAddress)
                        .addListener(
                            (ChannelFuture bind) -> {
                              if (bind.isSuccess()) {
                                final Channel channel = bind.channel();
                                logger.info("{} listening", channel);
                                channel
                                    .closeFuture()
                                    .addListener(
                                        (ChannelFuture closed) -> {
                                          if (closed.isSuccess()) {
                                            logger.debug("{} closed", closed.channel());
                                          } else {
                                            logger.error(
                                                "{} closed due to {}",
                                                closed.channel(),
                                                closed.cause().toString());
                                          }
                                        });
                              } else {
                                logger.error("bind failed due to {}", bind.cause().toString());
                              }
                            }));
              }

              combiner.finish(promise);
            });

    return promise;
  }

  private static void flushAndClose(Channel channel) {
    if (channel.isActive()) {
      logger.info("{} FLUSH AND CLOSE", channel);
      channel.writeAndFlush(Unpooled.EMPTY_BUFFER).addListener(ChannelFutureListener.CLOSE);
    }
  }

  private static class SourceHandler extends ChannelInboundHandlerAdapter {

    private final SocketAddress serviceAddress;
    private final SslContext sslContext;
    private Channel outboundChannel;

    SourceHandler(SocketAddress targetAddress, SslContext sslContext) {
      this.serviceAddress = targetAddress;
      this.sslContext = sslContext;
    }

    public void channelActive(final ChannelHandlerContext context) {

      logger.trace("{} channelActive", context.channel());
      final Channel inboundChannel = context.channel();

      Bootstrap bootstrap =
          new Bootstrap()
              .channel(context.channel().getClass())
              .group(inboundChannel.eventLoop())
              .handler(
                  new ChannelInitializer<SocketChannel>() {
                    @Override
                    protected void initChannel(SocketChannel outboundChannel) throws Exception {
                      outboundChannel.pipeline().removeFirst();
                      outboundChannel
                          .pipeline()
                          .addFirst(new TargetHandler(inboundChannel))
                          .addFirst(new SslHandler(sslContext.newEngine(outboundChannel.alloc())));
                    }
                  })
              .option(ChannelOption.AUTO_READ, false);

      ChannelFuture connect = bootstrap.connect(this.serviceAddress);
      this.outboundChannel = connect.channel();

      connect.addListener(
          (ChannelFutureListener)
              future -> {
                if (future.isSuccess()) {
                  logger.info("{} READ from {}", future.channel(), inboundChannel);
                  inboundChannel.read();
                } else {
                  logger.info("{} CLOSE due to {}", future.channel(), future.cause().toString());
                  inboundChannel.close();
                }
              });
    }

    @Override
    public void channelInactive(final ChannelHandlerContext context) {
      logger.trace("{} channelInactive", context.channel());
      if (this.outboundChannel != null) {
        flushAndClose(this.outboundChannel);
      }
    }

    @Override
    public void channelRead(final ChannelHandlerContext context, final Object message) {
      logger.trace("{} channelRead", context.channel());
      if (this.outboundChannel.isActive()) {
        this.outboundChannel
            .writeAndFlush(message)
            .addListener(
                (ChannelFutureListener)
                    future -> {
                      if (future.isSuccess()) {
                        context.channel().read();
                      } else {
                        future.channel().close();
                      }
                    });
      }
    }

    @Override
    public void exceptionCaught(final ChannelHandlerContext context, final Throwable cause) {
      logger.error("{} closing due to {}", context.channel(), cause.toString());
      flushAndClose(context.channel());
    }
  }

  public static class TargetHandler extends ChannelInboundHandlerAdapter {

    private final Channel inboundChannel;

    TargetHandler(Channel inboundChannel) {
      this.inboundChannel = inboundChannel;
    }

    @Override
    public void channelActive(final ChannelHandlerContext context) {
      logger.trace("{} channelActive", context.channel());
      context.read();
    }

    @Override
    public void channelInactive(final ChannelHandlerContext context) {
      logger.trace("{} channelInactive", context.channel());
      flushAndClose(context.channel());
    }

    @Override
    public void channelRead(final ChannelHandlerContext context, final Object message) {
      logger.trace("{} channelRead", context.channel());
      logger.info("{} WRITE to {}", context.channel(), this.inboundChannel);
      this.inboundChannel
          .writeAndFlush(message)
          .addListener(
              (ChannelFutureListener)
                  future -> {
                    if (future.isSuccess()) {
                      logger.info("{} READ", future.channel());
                      context.channel().read();
                    } else {
                      logger.info(
                          "{} WRITE to inbound channel {} failed due to {}",
                          future.channel(),
                          this.inboundChannel,
                          future.cause().toString());
                      future.channel().close();
                    }
                  });
    }

    @Override
    public void exceptionCaught(final ChannelHandlerContext context, final Throwable cause) {
      logger.error("{} closing due to {}", context.channel(), cause.toString());
      flushAndClose(context.channel());
    }
  }
}
