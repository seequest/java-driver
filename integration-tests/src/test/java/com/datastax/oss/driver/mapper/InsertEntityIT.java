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
package com.datastax.oss.driver.mapper;

import com.datastax.oss.driver.api.core.CqlSession;
import com.datastax.oss.driver.api.core.cql.Row;
import com.datastax.oss.driver.api.core.cql.SimpleStatement;
import com.datastax.oss.driver.api.testinfra.ccm.CcmRule;
import com.datastax.oss.driver.api.testinfra.session.SessionRule;
import com.datastax.oss.driver.categories.ParallelizableTests;
import com.datastax.oss.driver.mapper.model.inventory.InventoryFixtures;
import com.datastax.oss.driver.mapper.model.inventory.InventoryMapper;
import com.datastax.oss.driver.mapper.model.inventory.InventoryMapperBuilder;
import com.datastax.oss.driver.mapper.model.inventory.Product;
import com.datastax.oss.driver.mapper.model.inventory.ProductDao;
import org.junit.BeforeClass;
import org.junit.ClassRule;
import org.junit.Test;
import org.junit.experimental.categories.Category;
import org.junit.rules.RuleChain;
import org.junit.rules.TestRule;

@Category(ParallelizableTests.class)
public class InsertEntityIT {

  private static CcmRule ccm = CcmRule.getInstance();

  private static SessionRule<CqlSession> sessionRule = SessionRule.builder(ccm).build();

  @ClassRule public static TestRule chain = RuleChain.outerRule(ccm).around(sessionRule);

  private static ProductDao productDao;

  @BeforeClass
  public static void setup() {
    CqlSession session = sessionRule.session();

    for (String query : InventoryFixtures.createStatements()) {
      session.execute(
          SimpleStatement.builder(query).withExecutionProfile(sessionRule.slowProfile()).build());
    }

    InventoryMapper inventoryMapper = new InventoryMapperBuilder(session).build();
    productDao = inventoryMapper.productDao(sessionRule.keyspace());
  }

  @Test
  public void should_insert_entity() {
    // Given
    CqlSession session = sessionRule.session();

    // When
    Product product = InventoryFixtures.FLAMETHROWER.entity;
    productDao.save(product);
    Row row =
        session
            .execute(
                SimpleStatement.newInstance("SELECT * FROM product WHERE id = ?", product.getId()))
            .one();

    // Then
    InventoryFixtures.FLAMETHROWER.assertMatches(row);
  }
}
