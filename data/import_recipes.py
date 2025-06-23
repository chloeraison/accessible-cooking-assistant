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
import os # Relative path creation
from dotenv import load_dotenv # Security design

load_dotenv() # load_dotenv()  # Loads from .env
print("Loaded password:", os.getenv("DB_PASSWORD"))

# Load csv file via csv_path
csv_path = "C:/Users/cadmi/data/recipeData/RAW_recipes.csv"
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
        dbname=os.getenv("DB_NAME"),
        user=os.getenv("DB_USER"),
        password=os.getenv("DB_PASSWORD"),
        host=os.getenv("DB_HOST"),
        port=os.getenv("DB_PORT")
    )
    cursor = conn.cursor()
    print("Connected to PostgreSQL")
except Exception as e:
    print(f"Database connection failed: {e}")
    exit()

# Set counters to keep track of import success
success_count = 0
fail_count = 0 

#Testing the loop so as not to break the repo
TEST_MODE = True  # Set to False to import full dataset
rows = df.head(5) if TEST_MODE else df
# Loop through the recipe rows
for _, row in rows.iterrows(): # Limiting the number of recipes for now 
    name = row["name"].strip().lower()  # Standardise recipe name
    
    try:
        # Parse steps and insert to avoid database break
        steps = ast.literal_eval(row["steps"])  # Convert string to list

        for i, step in enumerate(steps):
            cursor.execute(
                "INSERT INTO recipes (name, step_number, instruction) VALUES (%s, %s, %s);",
                (name, i + 1, step.strip())
            )

    # Handle errors and log them
    except (ValueError, SyntaxError) as e:
        print(f"Skipping recipe '{name}' due to parsing error: {e}")
    except Exception as db_error:
        print(f"DB error on recipe '{name}': {db_error}")

# Commit, close and final output 'import complete' end of process
try:
    conn.commit()
    print("Import complete.")
except Exception as e:
    print(f"Commit failed: {e}")
finally:
    cursor.close()
    conn.close()