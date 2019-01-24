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
package com.datastax.oss.driver.api.core.cql;

import com.datastax.oss.driver.api.core.CqlSession;
import com.datastax.oss.driver.api.core.PagingIterable;
import edu.umd.cs.findbugs.annotations.NonNull;

/**
 * The result of a synchronous CQL query.
 *
 * <p>See {@link PagingIterable} for a few generic explanations about the behavior of this object;
 * in particular, implementations are <b>not thread-safe</b>. They can only be iterated by the
 * thread that invoked {@code session.execute}.
 *
 * @see CqlSession#execute(Statement)
 * @see CqlSession#execute(String)
 */
public interface ResultSet extends PagingIterable<Row> {

  /** @return the column definitions contained in this result set. */
  @NonNull
  ColumnDefinitions getColumnDefinitions();

  // overridden to amend the javadocs:
  /**
   * {@inheritDoc}
   *
   * <p>This is equivalent to calling:
   *
   * <pre>
   *   this.iterator().next().getBoolean("[applied]")
   * </pre>
   *
   * Except that this method peeks at the next row without consuming it.
   *
   * <p>For consistency, this method always returns {@code true} for non-conditional queries
   * (although there is no reason to call the method in that case). This is also the case for
   * conditional DDL statements ({@code CREATE KEYSPACE... IF NOT EXISTS}, {@code CREATE TABLE... IF
   * NOT EXISTS}), for which Cassandra doesn't return an {@code [applied]} column.
   *
   * <p>Note that, for versions of Cassandra strictly lower than 2.1.0-rc2, a server-side bug (<a
   * href="https://issues.apache.org/jira/browse/CASSANDRA-7337">CASSANDRA-7337</a>) causes this
   * method to always return {@code true} for batches containing conditional queries.
   */
  @Override
  boolean wasApplied();
}
