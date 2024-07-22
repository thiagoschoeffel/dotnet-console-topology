# How to run

*To run this project, you will need to have Python and Docker installed.*

First we must enter the Utils folder and execute the commands:

`cd ConsoleSimulationRedis/Utils` and `pip install psycopg2-binary shapely` to install Python dependencies.

Now, we need to run the Postgis and Redis container:

Postgis:
`docker run --name postgis -e POSTGRES_PASSWORD=postgis -d -p 5432:5432 postgis/postgis`

Redis:
`docker run --name redis -d -p 6379:6379 redis`

Let's access our Postgis database and within the public schema create our database and table:

**Database:**
`CREATE DATABASE geometries;`

**Postgis Extension:**
`CREATE EXTENSION postgis;`

**Table:**
`
CREATE TABLE geometries (
    id SERIAL PRIMARY KEY,
    geom GEOMETRY(MultiPolygon, 4326),
    created_at TIMESTAMP
);
`

Let's run the python script to populate the geometry table with 100 thousand random records.

`cd ConsoleSimulationRedis/Utils` and execute `python FakerTerritories.py`

### 🚀 We already have everything ready to execute the project!

Example:

Database with fake geometries:

![image](https://github.com/user-attachments/assets/c742a939-849b-459b-b4cd-09b502e0249c)

Exectution with data from database:

![image](https://github.com/user-attachments/assets/900d9471-1eba-4811-b641-365ff0c0208a)

Execution with data from cache:

![image](https://github.com/user-attachments/assets/5578a0f9-d0ee-4d8e-8b10-be88638d4800)
