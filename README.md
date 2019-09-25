A library to implement state based schema updates in PostgreSQL.

Just describe the data schema and let the library to generate the SQL to update the schema of an existing PostgreSQL instance.

See the included samples for instructions on how to use it.

**Warning:** This library can be used to make an SQL injection attack. It's supposed that the schema are setup by a developer in source code. DO NOT USE THIS LIBRARY WITH USER INPUT.
