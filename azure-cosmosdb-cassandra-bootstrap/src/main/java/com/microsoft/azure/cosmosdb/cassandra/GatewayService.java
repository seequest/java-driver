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
import static org.slf4j.LoggerFactory.getLogger;

import com.google.common.base.Stopwatch;
import com.google.common.util.concurrent.AbstractService;
import com.google.common.util.concurrent.Service;
import io.netty.util.concurrent.GlobalEventExecutor;
import java.io.File;
import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.nio.file.attribute.PosixFilePermission;
import java.util.Arrays;
import java.util.EnumSet;
import java.util.Set;
import java.util.concurrent.TimeUnit;
import org.apache.commons.compress.archivers.tar.TarArchiveEntry;
import org.apache.commons.compress.archivers.tar.TarArchiveInputStream;
import org.apache.commons.compress.compressors.gzip.GzipCompressorInputStream;
import org.slf4j.Logger;

final class GatewayService extends AbstractService {

  public static final String name = "azure-cosmosdb-cassandra-gateway";

  private static final String className = GatewayService.class.getCanonicalName();
  private static final String displayName = "Cosmos DB Cassandra Gateway service";
  private static final Stopwatch lifetime = Stopwatch.createStarted();
  private static final Logger logger = getLogger(className);
  private static final ProcessBuilder serviceProcess;

  static {
    final File classesRoot =
        new File(
            GatewayService.class.getProtectionDomain().getCodeSource().getLocation().getPath());

    final String[] names =
        classesRoot.list(
            (dir, name) ->
                name.startsWith(GatewayService.name) && name.endsWith("-service.tar.gz"));

    checkState(names != null && names.length == 1);

    final String defaultInstallDirectory =
        Paths.get(System.getProperty("user.home"), "local", "opt").toString();

    final String installRoot =
        Paths.get(System.getProperty(GatewayService.className, defaultInstallDirectory)).toString();

    final String serviceHome =
        names[0].substring(0, names[0].length() - "-service.tar.gz".length());

    final File servicePath =
        Paths.get(installRoot, serviceHome, "bin", GatewayService.name)
            .normalize()
            .toAbsolutePath()
            .toFile();

    final String os = System.getProperty("os.name");
    final String resourceName = '/' + names[0];

    serviceProcess =
        os.startsWith("Windows")
            ? new ProcessBuilder("cmd.exe", "/c", servicePath.toString()).inheritIO()
            : new ProcessBuilder(servicePath.toString()).inheritIO();

    serviceProcess
        .environment()
        .put(
            "JAVA_OPTS",
            Arrays.stream(
                    new String[] {
                      "cosmos.gatewayService.host",
                      "cosmos.gatewayService.port",
                      "cosmos.gatewayProxy.host",
                      "cosmos.gatewayProxy.port",
                      "javax.net.ssl.trustStore",
                      "javax.net.ssl.trustStoreType",
                      "javax.net.ssl.trustStorePassword",
                    })
                .reduce(
                    "",
                    (result, name) -> {
                      String value = System.getProperty(name);
                      return value == null ? result : result + " -D" + name + '=' + value;
                    }));

    if (!servicePath.exists()) {

      try (TarArchiveInputStream archive =
          new TarArchiveInputStream(
              new GzipCompressorInputStream(
                  GatewayService.class.getResourceAsStream(resourceName)))) {

        final Set<PosixFilePermission> permissions = EnumSet.noneOf(PosixFilePermission.class);
        TarArchiveEntry entry;

        while ((entry = archive.getNextTarEntry()) != null) {

          if (entry.isDirectory()) {
            continue;
          }

          File file = new File(installRoot, entry.getName());
          File parent = file.getParentFile();

          if (!parent.exists()) {
            parent.mkdirs();
          }

          final Path target = file.toPath();
          final int mode = entry.getMode();

          final int owner = (mode & 0700) >> 6;

          if ((owner & 1) != 0) {
            permissions.add(PosixFilePermission.OWNER_EXECUTE);
          }
          if ((owner & 2) != 0) {
            permissions.add(PosixFilePermission.OWNER_WRITE);
          }
          if ((owner & 4) != 0) {
            permissions.add(PosixFilePermission.OWNER_READ);
          }

          final int group = (mode & 0070) >> 3;

          if ((group & 1) != 0) {
            permissions.add(PosixFilePermission.GROUP_EXECUTE);
          }
          if ((group & 2) != 0) {
            permissions.add(PosixFilePermission.GROUP_WRITE);
          }
          if ((group & 4) != 0) {
            permissions.add(PosixFilePermission.GROUP_READ);
          }

          final int others = (mode & 0007);

          if ((others & 1) != 0) {
            permissions.add(PosixFilePermission.OTHERS_EXECUTE);
          }
          if ((others & 2) != 0) {
            permissions.add(PosixFilePermission.OTHERS_WRITE);
          }
          if ((others & 4) != 0) {
            permissions.add(PosixFilePermission.OTHERS_READ);
          }

          Files.copy(archive, target);
          Files.setPosixFilePermissions(target, permissions);
          permissions.clear();
        }
      } catch (final IOException error) {
        logger.error("cannot decompress {} due to {}", resourceName, error.toString());
      }
    }

    logger.info("INITIALIZED: {} ms", lifetime.elapsed(TimeUnit.MILLISECONDS));
  }

  private Process process;

  GatewayService() {

    this.addListener(new GatewayService.Listener(), GlobalEventExecutor.INSTANCE);

    Runtime.getRuntime()
        .addShutdownHook(
            new Thread(
                () -> {
                  synchronized (this) {
                    if (this.process != null && this.process.isAlive()) {
                      this.process.destroyForcibly();
                    }
                  }
                }));
  }

  String displayName() {
    return displayName;
  }

  String name() {
    return name;
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
                  this.process = serviceProcess.start();
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
     * Called when the service transitions from {@linkplain State#NEW NEW} to {@linkplain
     * State#STARTING STARTING}. This occurs when {@link Service#startAsync} is called the first
     * time.
     */
    @Override
    public void starting() {
      logger.debug("{} is starting", displayName);
    }

    /**
     * Called when the service transitions from {@linkplain State#STARTING STARTING} to {@linkplain
     * State#RUNNING RUNNING}. This occurs when a service has successfully started.
     */
    @Override
    public void running() {
      logger.debug("{} is running", displayName);
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
      logger.debug("{} is stopping", displayName);
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
      logger.debug("{} is terminated", displayName);
    }

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
      logger.error("{} failed in {} state: {}", displayName, from, failure.toString());
    }
  }
}
