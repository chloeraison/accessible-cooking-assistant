# *~*~*~*~*~*~*~*~*
# Script: import_recipes.py
# Purpose: Load and import recipe data from a CSV file in PostgreSQL database
# 
# Description:
# 1) Loads reciper data from the RAW_recipes.csv file from kaggle, named 'Food.com Recipes and Interactions'
# 2) Parses the 'steps' column (which contains a list of steps)
# 3) Connect to local PostgreSQL database (accessible_recipes)
# 4) Breaks each recipe into individual step rows
# 5) Inserts those rows into the 'recipes' table using:
#   - Name
#   - step_number
#   - Instruction
# 
# Basic error handling is included to skip bad entries
# *~*~*~*~*~*~*~*~*

import psycopg2 # connection for Python to PostgreSQL
import pandas as pd # Loads and reads the raw recipe CSV
import ast # Turns string version of python list (['point 1', 'point 2'] into an actual list
import os # Relative path creation so the code is usable on different device

# Load csv file via csv_path
csv_path = os.path.join("C:/Users/cadmi/data/recipeData/RAW_recipes.csv")
df = pd.read_csv(csv_path)

# Test to see if the files are loaded:
""" try:
    df = pd.read_csv(csv_path)
    print("Loaded successfully")
    print("Some rows as example:", df.head(3))
except Exception as e:
    print(f"failed: {e}") """

# Connect to postgreSQL
try:
    conn = psycopg2.connect(
        dbname="accessible_recipes",
        user="postgres",
        password="postgres",
        host="localhost",
        port="5432"
    )
    cursor = conn.cursor()
    print("Connected to PostgreSQL")
except Exception as e:
    print(f"Database connection failed: {e}")
    exit()

# Set counters to keep track of import success

# Loop through the recipe rows

# Parse steps and insert to avoid database break

# Insert steps into the tables so no one's putting their flour in the oven without the other elements of the cookie batter

# Handle errors and log them

# Commit, close and final output 'import complete' end of process