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

import static java.lang.System.exit;

import java.io.IOException;
import java.net.InetAddress;
import java.net.InetSocketAddress;
import java.net.SocketAddress;
import java.net.UnknownHostException;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

public final class GatewayProxyHost {

  private static final Logger logger = LoggerFactory.getLogger(GatewayProxyHost.class);
  private static final String name = "Cosmos Cassandra Gateway service host";

  public static void main(String[] args) {

    SocketAddress serviceAddress = getSocketAddress("cassandra.GatewayService", 9142);
    SocketAddress proxyAddress = getSocketAddress("cassandra.GatewayProxy", 9042);

    logger.info("starting listener for CQL clients on {}", proxyAddress);
    logger.info("inbound messages will pass-through to Cassandra node on {}", serviceAddress);

    try (GatewayProxy proxy = new GatewayProxy()) {
      proxy.start(proxyAddress, serviceAddress);
      while (System.in.read() > 0) {}
    } catch (IOException error) {
      exit(1);
    }

    exit(0);
  }

  private static SocketAddress getSocketAddress(final String name, int defaultPort) {

    final String host = System.getProperty(name + ".host", "localhost");
    InetAddress[] inetAddresses = null;

    try {
      inetAddresses = InetAddress.getAllByName(host);
    } catch (UnknownHostException error) {
      logger.error("{}.host: Unknown host: {}", name, host);
      exit(1);
    }

    final String port = System.getProperty(name + ".port", Integer.toString(defaultPort));
    int portNumber = 0;

    try {
      portNumber = Integer.parseInt(port);
    } catch (NumberFormatException error) {
      logger.error("{}.port: Expected integer value, not {}", name, port);
      exit(1);
    }

    SocketAddress socketAddress = null;

    try {
      socketAddress = new InetSocketAddress(inetAddresses[0], portNumber);
    } catch (IllegalArgumentException error) {
      logger.error("{}.port: Expected integer value in range of [0, 65535], not {}", name, port);
      exit(1);
    }

    return socketAddress;
  }
}
