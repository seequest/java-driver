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
package com.datastax.oss.driver.mapper.model.udts;

import com.datastax.oss.driver.api.mapper.annotations.Entity;
import java.util.List;
import java.util.Map;
import java.util.Set;
import java.util.UUID;

@Entity
public class Container {

  private UUID id;
  private List<Type1> list;
  private Map<String, List<Type1>> map1;
  private Map<Type1, Set<List<Type2>>> map2;

  public Container() {}

  public Container(
      UUID id, List<Type1> list, Map<String, List<Type1>> map1, Map<Type1, Set<List<Type2>>> map2) {
    this.id = id;
    this.list = list;
    this.map1 = map1;
    this.map2 = map2;
  }

  public UUID getId() {
    return id;
  }

  public void setId(UUID id) {
    this.id = id;
  }

  public List<Type1> getList() {
    return list;
  }

  public void setList(List<Type1> list) {
    this.list = list;
  }

  public Map<String, List<Type1>> getMap1() {
    return map1;
  }

  public void setMap1(Map<String, List<Type1>> map1) {
    this.map1 = map1;
  }

  public Map<Type1, Set<List<Type2>>> getMap2() {
    return map2;
  }

  public void setMap2(Map<Type1, Set<List<Type2>>> map2) {
    this.map2 = map2;
  }
}
