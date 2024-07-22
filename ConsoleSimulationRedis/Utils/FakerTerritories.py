import psycopg2
from psycopg2.extras import execute_values
from shapely.geometry import MultiPolygon, Polygon
from shapely.wkt import dumps as to_wkt
from shapely.validation import make_valid
import random
from datetime import datetime, timedelta
import numpy as np

# Configurações do banco de dados
conn = psycopg2.connect(
    dbname="geometries",
    user="postgres",
    password="postgis",
    host="localhost"
)
cur = conn.cursor()

def random_date(start_year=2001, end_year=2024):
    """Gera uma data aleatória entre os anos start_year e end_year."""
    start_date = datetime(start_year, 1, 1)
    end_date = datetime(end_year, 12, 31)
    delta = end_date - start_date
    random_days = random.randint(0, delta.days)
    return start_date + timedelta(days=random_days)

def generate_polygon_with_area(min_area, max_area):
    """Gera um polígono aleatório com uma área dentro do intervalo especificado."""
    while True:
        num_points = random.randint(5, 10)
        radius = np.sqrt(random.uniform(min_area, max_area) / (np.pi * 10000))  # Convert m² to km²

        # Cria um anel de pontos em torno de um centro com o raio especificado
        center = (random.uniform(-70, -50), random.uniform(-10, 10))
        ring = [(center[0] + radius * np.cos(2 * np.pi * i / num_points),
                 center[1] + radius * np.sin(2 * np.pi * i / num_points)) for i in range(num_points)]
        ring.append(ring[0])  # Fecha o polígono
        
        polygon = Polygon(ring)
        polygon = make_valid(polygon)
        
        # Verifica se a área está dentro do intervalo desejado
        area = polygon.area * 10000  # Convert km² to m²
        if min_area <= area <= max_area:
            return polygon

def generate_random_multipolygon(min_area, max_area):
    """Gera um MultiPolygon aleatório com áreas dentro do intervalo especificado."""
    num_polygons = random.randint(1, 5)
    polygons = [generate_polygon_with_area(min_area, max_area) for _ in range(num_polygons)]
    multipolygon = MultiPolygon(polygons)
    multipolygon = make_valid(multipolygon)
    return multipolygon

# Gera dados fictícios
min_area = 30000  # Área mínima em m²
max_area = 200000  # Área máxima em m²
data = []
for _ in range(100000):  # Inserir 100.000 registros
    geom = generate_random_multipolygon(min_area, max_area)
    created_at = random_date()
    data.append((to_wkt(geom), created_at))

# Insere dados na tabela
insert_query = """
INSERT INTO geometries (geom, created_at)
VALUES %s
"""
execute_values(cur, insert_query, data)

# Commit e fechamento
conn.commit()
cur.close()
conn.close()
