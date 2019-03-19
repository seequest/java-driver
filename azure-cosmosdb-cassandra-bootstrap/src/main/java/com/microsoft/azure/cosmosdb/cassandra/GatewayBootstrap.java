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

import io.netty.bootstrap.Bootstrap;
import io.netty.channel.Channel;
import io.netty.channel.ChannelFuture;
import java.net.SocketAddress;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

public final class GatewayBootstrap extends Bootstrap {

  private static final Logger logger = LoggerFactory.getLogger(GatewayBootstrap.class);
  private static final GatewayService service = new GatewayService();

  /**
   * Connect a {@link Channel} to the remote peer.
   *
   * @param remoteAddress address of gateway to which this {@link GatewayBootstrap} should connect a
   *     channel
   */
  @Override
  public ChannelFuture connect(SocketAddress remoteAddress) {
    this.ensureRunning();
    return super.connect(remoteAddress);
  }

  /**
   * Connect a {@link Channel} to the remote peer.
   *
   * @param remoteAddress address of gateway to which this {@link GatewayBootstrap} should connect a
   *     channel
   * @param localAddress local address to which the connected channel should bind
   */
  @Override
  public ChannelFuture connect(SocketAddress remoteAddress, SocketAddress localAddress) {
    this.ensureRunning();
    return super.connect(remoteAddress, localAddress);
  }

  private void ensureRunning() {

    switch (service.state()) {
      case FAILED:
        String message =
            String.format("%s encountered a problem and may not be operational", service.name());
        logger.error(message);
        throw new IllegalStateException();
      case NEW:
        break;
      case RUNNING:
        return;
      case STARTING:
        service.awaitRunning();
        return;
      case STOPPING:
        service.awaitTerminated();
        break;
      case TERMINATED:
        break;
    }

    service.startAsync().awaitRunning();
  }
}
