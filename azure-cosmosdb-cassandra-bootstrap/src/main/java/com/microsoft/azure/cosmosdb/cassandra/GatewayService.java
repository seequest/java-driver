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

import com.google.common.util.concurrent.AbstractService;
import com.google.common.util.concurrent.Service;
import io.netty.util.concurrent.GlobalEventExecutor;
import java.io.IOException;
import java.net.URL;
import java.util.Map;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

final class GatewayService extends AbstractService {

  private static final Logger logger = LoggerFactory.getLogger(GatewayService.class);
  private static final String name = "Cosmos Cassandra Gateway service";
  private static final ProcessBuilder command;

  static {
    final String gatewayServicePropertyName =
        GatewayService.class.getPackage().getName() + ".GatewayService";
    final URL script = GatewayService.class.getResource("/Start-CassandraGatewayService");
    command = new ProcessBuilder(script.getPath());

    Map<String, String> environment = command.environment();
    environment.put("JAVA_HOME", System.getProperty("java.home"));
    environment.put("CASSANDRA_GATEWAY_SERVICE", System.getProperty(gatewayServicePropertyName));

    command.inheritIO();
  }

  private Process process;

  GatewayService() {
    this.addListener(new GatewayService.Listener(), GlobalEventExecutor.INSTANCE);
    process = null;
  }

  String name() {
    return GatewayService.name;
  }

  /**
   * This method is called by {@link #startAsync} to initiate service startup. The invocation of
   * this method should cause a call to {@link #notifyStarted()}, either during this method's run,
   * or after it has returned. If startup fails, the invocation should cause a call to {@link
   * #notifyFailed(Throwable)} instead.
   *
   * <p>This method should return promptly; prefer to do work on a different thread where it is
   * convenient. It is invoked exactly once on service startup, even when {@link #startAsync} is
   * called multiple times.
   */
  @Override
  protected void doStart() {
    GlobalEventExecutor.INSTANCE
        .next()
        .submit(
            () -> {
              synchronized (this) {
                try {
                  this.process = command.start();
                  this.notifyStarted();
                } catch (IOException error) {
                  this.notifyFailed(error);
                }
              }
            });
  }

  /**
   * This method should be used to initiate service shutdown. The invocation of this method should
   * cause a call to {@link #notifyStopped()}, either during this method's run, or after it has
   * returned. If shutdown fails, the invocation should cause a call to {@link
   * #notifyFailed(Throwable)} instead.
   *
   * <p>This method should return promptly; prefer to do work on a different thread where it is
   * convenient. It is invoked exactly once on service shutdown, even when {@link #stopAsync} is
   * called multiple times.
   */
  @Override
  protected void doStop() {

    checkState(this.process != null);
    checkState(this.process.isAlive());

    GlobalEventExecutor.INSTANCE
        .next()
        .submit(
            () -> {
              synchronized (this) {
                try {
                  this.process.destroyForcibly().waitFor();
                  this.notifyStopped();
                } catch (InterruptedException error) {
                  this.notifyFailed(error);
                }
              }
            });
  }

  @Override
  public String toString() {
    return super.toString();
  }

  static class Listener extends Service.Listener {

    /**
     * Called when the service transitions to the {@linkplain State#FAILED FAILED} state. The
     * {@linkplain State#FAILED FAILED} state is a terminal state in the transition diagram.
     * Therefore, if this method is called, no other methods will be called on the {@link Listener}.
     *
     * @param from The previous state that is being transitioned from. Failure can occur in any
     *     state with the exception of {@linkplain State#NEW NEW} or {@linkplain State#TERMINATED
     *     TERMINATED}.
     * @param failure The exception that caused the failure.
     */
    @Override
    public void failed(State from, Throwable failure) {
      GatewayService.logger.error("{} failed in {} state: {}", name, from, failure);
    }

    /**
     * Called when the service transitions from {@linkplain State#STARTING STARTING} to {@linkplain
     * State#RUNNING RUNNING}. This occurs when a service has successfully started.
     */
    @Override
    public void running() {
      GatewayService.logger.debug("{} is running", name);
    }

    /**
     * Called when the service transitions from {@linkplain State#NEW NEW} to {@linkplain
     * State#STARTING STARTING}. This occurs when {@link Service#startAsync} is called the first
     * time.
     */
    @Override
    public void starting() {
      GatewayService.logger.debug("{} is starting", name);
    }

    /**
     * Called when the service transitions to the {@linkplain State#STOPPING STOPPING} state. The
     * only valid values for {@code from} are {@linkplain State#STARTING STARTING} or {@linkplain
     * State#RUNNING RUNNING}. This occurs when {@link Service#stopAsync} is called.
     *
     * @param from The previous state that is being transitioned from.
     */
    @Override
    public void stopping(State from) {
      GatewayService.logger.debug("{} is stopping", name);
    }

    /**
     * Called when the service transitions to the {@linkplain State#TERMINATED TERMINATED} state.
     * The {@linkplain State#TERMINATED TERMINATED} state is a terminal state in the transition
     * diagram. Therefore, if this method is called, no other methods will be called on the {@link
     * Listener}.
     *
     * @param from The previous state that is being transitioned from. The only valid values for
     *     this are {@linkplain State#NEW NEW}, {@linkplain State#RUNNING RUNNING} or {@linkplain
     *     State#STOPPING STOPPING}.
     */
    @Override
    public void terminated(State from) {
      GatewayService.logger.debug("{} is terminated", name);
    }
  }
}
