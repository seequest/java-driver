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
package com.datastax.driver.examples.concurrent;

import static com.datastax.oss.driver.api.querybuilder.QueryBuilder.bindMarker;
import static com.datastax.oss.driver.api.querybuilder.QueryBuilder.insertInto;

import com.datastax.oss.driver.api.core.CqlSession;
import com.datastax.oss.driver.api.core.CqlSessionBuilder;
import com.datastax.oss.driver.api.core.cql.AsyncResultSet;
import com.datastax.oss.driver.api.core.cql.PreparedStatement;
import com.datastax.oss.driver.internal.core.util.concurrent.CompletableFutures;
import java.util.HashSet;
import java.util.Set;
import java.util.UUID;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.CountDownLatch;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.atomic.AtomicInteger;

public class LimitConcurrency {
  private static final int CONCURRENCY_LEVEL = 32;
  private static final int TOTAL_NUMBER_OF_INSERTS = 10_000;

  public static void main(String[] args) throws InterruptedException {

    // The Session is what you use to execute queries. It is thread-safe and should be
    // reused.
    try (CqlSession session = new CqlSessionBuilder().build()) {
      createSchema(session);
      insertConcurrent(session);
    }
  }

  private static void insertConcurrent(CqlSession session) throws InterruptedException {
    PreparedStatement pst =
        session.prepare(
            insertInto("examples", "tbl_sample_kv")
                .value("id", bindMarker("id"))
                .value("value", bindMarker("value"))
                .build());

    CountDownLatch requestLatch = new CountDownLatch(TOTAL_NUMBER_OF_INSERTS);
    ExecutorService executor = Executors.newFixedThreadPool(CONCURRENCY_LEVEL);

    AtomicInteger insertsCounter = new AtomicInteger();
    Set<String> threads = new HashSet<>();

    for (int i = 0; i < TOTAL_NUMBER_OF_INSERTS; i++) {
      int counter = i;
      CompletableFuture.supplyAsync(
          () -> {
            insertsCounter.incrementAndGet();
            threads.add(Thread.currentThread().getName());
            CompletableFuture<? extends AsyncResultSet> completableFuture =
                session
                    .executeAsync(
                        pst.bind()
                            .set("id", UUID.randomUUID(), UUID.class)
                            .set("value", String.format("Value for: %s", counter), String.class))
                    .toCompletableFuture();
            AsyncResultSet executedRequest =
                CompletableFutures.getUninterruptibly(completableFuture);
            requestLatch.countDown();
            return executedRequest;
          },
          executor);
    }
    requestLatch.await();

    System.out.println(
        String.format(
            "Finished executing %s queries with a concurrency level of %s.",
            insertsCounter.get(), threads.size()));
  }

  private static void createSchema(CqlSession session) {
    session.execute(
        "CREATE KEYSPACE IF NOT EXISTS examples "
            + "WITH replication = {'class': 'SimpleStrategy', 'replication_factor': 1}");

    session.execute(
        "CREATE TABLE IF NOT EXISTS examples.tbl_sample_kv (id uuid, value text, PRIMARY KEY (id))");
  }
}
