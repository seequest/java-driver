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
package com.datastax.oss.driver.examples.concurrent;

import static com.datastax.oss.driver.api.querybuilder.QueryBuilder.bindMarker;
import static com.datastax.oss.driver.api.querybuilder.QueryBuilder.insertInto;

import com.datastax.oss.driver.api.core.CqlSession;
import com.datastax.oss.driver.api.core.CqlSessionBuilder;
import com.datastax.oss.driver.api.core.cql.AsyncResultSet;
import com.datastax.oss.driver.api.core.cql.PreparedStatement;
import java.util.ArrayList;
import java.util.List;
import java.util.UUID;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.ExecutionException;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.Semaphore;
import java.util.concurrent.atomic.AtomicInteger;
import java.util.function.BiConsumer;

/**
 * Creates a keyspace and tables, and loads data using Async API into them.
 *
 * <p>This example makes usage of a {@link CqlSession#executeAsync(String)} method, which is
 * responsible for executing requests in a non-blocking way. It uses {@link ExecutorService} to
 * limit number of concurrent request to {@code CONCURRENCY_LEVEL}. It maintains at most {@code
 * IN_FLIGHT_REQUESTS} using {@link Semaphore}.
 *
 * <p>Preconditions:
 *
 * <ul>
 *   <li>An Apache Cassandra(R) cluster is running and accessible through the contacts points
 *       identified by basic.contact-points (see application.conf).
 * </ul>
 *
 * <p>Side effects:
 *
 * <ul>
 *   <li>creates a new keyspace "examples" in the session. If a keyspace with this name already
 *       exists, it will be reused;
 *   <li>creates a table "examples.tbl_sample_kv". If it exist already, it will be reused;
 *   <li>inserts a TOTAL_NUMBER_OF_INSERTS of rows into the table.
 * </ul>
 *
 * @see <a href="http://datastax.github.io/java-driver/manual/">Java driver online manual</a>
 */
public class LimitConcurrencyCustomAsync {
  private static final int CONCURRENCY_LEVEL = 32;
  private static final int TOTAL_NUMBER_OF_INSERTS = 10_000;
  private static final int IN_FLIGHT_REQUESTS = 500;
  // Semaphore for limiting number of in-flight requests.
  private static final Semaphore SEMAPHORE = new Semaphore(IN_FLIGHT_REQUESTS);

  public static void main(String[] args) throws InterruptedException, ExecutionException {

    try (CqlSession session = new CqlSessionBuilder().build()) {
      createSchema(session);
      insertConcurrent(session);
    }
  }

  private static void insertConcurrent(CqlSession session)
      throws InterruptedException, ExecutionException {
    PreparedStatement pst =
        session.prepare(
            insertInto("examples", "tbl_sample_kv")
                .value("id", bindMarker("id"))
                .value("value", bindMarker("value"))
                .build());

    // Used to track number of total inserts
    AtomicInteger insertsCounter = new AtomicInteger();

    // Executor service with CONCURRENCY_LEVEL number of threads that states an upper limit
    // on number of request in progress.
    ExecutorService executor = Executors.newFixedThreadPool(CONCURRENCY_LEVEL);

    List<CompletableFuture<?>> pending = new ArrayList<>();

    // For every i we will insert a record to db
    for (int i = 0; i < TOTAL_NUMBER_OF_INSERTS; i++) {
      // Before submitting a request, we need to acquire 1 permit.
      // If there is no permits available it blocks caller thread.
      SEMAPHORE.acquire();
      // Copy to final variable for usage in a separate thread
      final int counter = i;

      // We are running CqlSession.executeAsync in a separate thread pool (executor)
      pending.add(
          CompletableFuture.supplyAsync(
                  () -> {
                    insertsCounter.incrementAndGet();
                    return executeInsert(session, pst, counter);
                  },
                  // Here the separate thread pool is passed as the argument
                  executor)
              .whenComplete(
                  (BiConsumer<CompletableFuture<? extends AsyncResultSet>, Throwable>)
                      (completableFuture, throwable) -> {
                        // Once the request is executed, we release 1 permit.
                        // By doing so we allow caller thread to submit another async request.
                        SEMAPHORE.release();
                        System.out.println("SEMAPHRE release: " + SEMAPHORE.availablePermits());
                      }));
    }

    CompletableFuture.allOf(pending.toArray(new CompletableFuture[0])).get();

    System.out.println(
        String.format(
            "Finished executing %s queries with a concurrency level of %s.",
            insertsCounter.get(), CONCURRENCY_LEVEL));
    // Shutdown executor to free resources
    executor.shutdown();
  }

  private static CompletableFuture<? extends AsyncResultSet> executeInsert(
      CqlSession session, PreparedStatement pst, int counter) {
    return session
        .executeAsync(pst.bind().setUuid("id", UUID.randomUUID()).setInt("value", counter))
        .toCompletableFuture();
  }

  private static void createSchema(CqlSession session) {
    session.execute(
        "CREATE KEYSPACE IF NOT EXISTS examples "
            + "WITH replication = {'class': 'SimpleStrategy', 'replication_factor': 1}");

    session.execute(
        "CREATE TABLE IF NOT EXISTS examples.tbl_sample_kv (id uuid, value int, PRIMARY KEY (id))");
  }
}
