CREATE KEYSPACE keyspace1 WITH replication = {'class': 'SimpleStrategy', 'replication_factor': '4'} AND durable_writes = true;

CREATE TABLE keyspace1.counter1 (
    key blob PRIMARY KEY
) WITH cosmosdb_provisioned_throughput=400;

CREATE TABLE keyspace1.standard1 (
    key blob PRIMARY KEY,
    "C0" blob,
    "C1" blob,
    "C2" blob,
    "C3" blob,
    "C4" blob
) WITH cosmosdb_provisioned_throughput=350000;

