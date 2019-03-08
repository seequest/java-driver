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
import java.util.concurrent.ExecutionException;
import java.util.concurrent.atomic.AtomicInteger;
import java.util.stream.IntStream;

public class LimitConcurrency {
  private static final int CONCURRENCY_LEVEL = 32;
  private static final int TOTAL_NUMBER_OF_INSERTS = 10_000;

  static {
    System.setProperty("java.util.concurrent.ForkJoinPool.common.parallelism", "32");
  }

  public static void main(String[] args) throws ExecutionException, InterruptedException {

    // The Session is what you use to execute queries. It is thread-safe and should be
    // reused.
    try (CqlSession session = new CqlSessionBuilder().build()) {
      createSchema(session);
      insertConcurrent(session);
    }
  }

  private static void insertConcurrent(CqlSession session)
      throws ExecutionException, InterruptedException {
    PreparedStatement pst =
        session.prepare(
            insertInto("examples", "tbl_sample_kv")
                .value("id", bindMarker("id"))
                .value("value", bindMarker("value"))
                .build());

    IntStream range = IntStream.range(0, TOTAL_NUMBER_OF_INSERTS);
    AtomicInteger insertsCounter = new AtomicInteger();
    Set<String> threads = new HashSet<>();

    range
        .parallel()
        .forEach(
            counter -> {
              insertsCounter.incrementAndGet();
              System.out.println("insert from Thread: " + Thread.currentThread().getName());
              System.out.println("Active threads: " + Thread.activeCount());
              threads.add(Thread.currentThread().getName());
              System.out.println(String.format("Value for: %s", counter));
              CompletableFuture<? extends AsyncResultSet> completableFuture =
                  session
                      .executeAsync(
                          pst.bind()
                              .set("id", UUID.randomUUID(), UUID.class)
                              .set("value", String.format("Value for: %s", counter), String.class))
                      .toCompletableFuture();
              CompletableFutures.getUninterruptibly(completableFuture);
            });

    System.out.println(insertsCounter.get());
    System.out.println(threads.size());
  }

  private static void createSchema(CqlSession session) {
    session.execute(
        "CREATE KEYSPACE IF NOT EXISTS examples "
            + "WITH replication = {'class': 'SimpleStrategy', 'replication_factor': 1}");

    session.execute(
        "CREATE TABLE IF NOT EXISTS examples.tbl_sample_kv (id uuid, value text, PRIMARY KEY (id))");
  }
}
