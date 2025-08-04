# *~*~*~*~*~*~*~*~*~*~*~*~*~*~*~*
# Script: import_recipes.py
# Purpose: Populate accessible cooking database with structured recipe data.
#
# What it does:
# 1. Loads recipes from the Kaggle “Food.com Recipes and Interactions” CSV.
# 2. Reads each recipe’s ingredient list and step list (both stored as Python-style text).
# 3. Connects securely to the PostgreSQL “accessible_recipes” database.
# 4. Inserts:
#     • One row in 'recipes' per dish (capturing its name).
#     • One row in 'ingredients' per item in the ingredient list.
#     • One row in 'steps' per instruction in the step list.
# 5. Skips any malformed entries without stopping and/or breaking the entire import.
#
# Usage:
#  - Toggle 'TEST_MODE' to True for a quick dry-run (first 5 recipes),
#    or False to load the full dataset.
#
# After running, there will be a fully populated `recipes`, `ingredients`, and `steps` schema
# to power voice-guided cooking, conversions, timers, and more.
# *~*~*~*~*~*~*~*~*~*~*~*~*

# import_recipes.py
import os
import ast
import psycopg2
import pandas as pd
from dotenv import load_dotenv

load_dotenv()   # loads DB_HOST, DB_PORT, DB_NAME, DB_USER, DB_PASSWORD

# Path to  CSV (in future make this an env var)
csv_path = "C:/Users/cadmi/data/recipeData/RAW_recipes.csv"

# Load the first few rows to test, all rows when TEST_MODE=False
TEST_MODE = True
df = pd.read_csv(csv_path)
rows = df.head(5) if TEST_MODE else df

# Connect
conn = psycopg2.connect(
    host=os.getenv("DB_HOST", "localhost"),
    port=os.getenv("DB_PORT", 5432),
    dbname=os.getenv("DB_NAME"),
    user=os.getenv("DB_USER"),
    password=os.getenv("DB_PASSWORD"),
)
cursor = conn.cursor()

success = fail = 0

for _, row in rows.iterrows():
    name = row["name"].strip()
    try:
        # 1) Insert into recipes
        cursor.execute(
            "INSERT INTO recipes (name) VALUES (%s) RETURNING recipe_id;",
            (name,)
        )
        recipe_id = cursor.fetchone()[0]

           # 2) Ingredients: parse a Python list stored as text
        try:
            raw_ings = ast.literal_eval(row["ingredients"])
        except Exception:
            raw_ings = []

        for raw in raw_ings:
            raw = raw.strip() # Safegaurd added for empty or malformed strings in data
            if not raw:
                continue

            # Try to split into [quantity, unit, name]
            parts = raw.split(" ", 2)
            if len(parts) == 3:
                qty_str, unit, name = parts
                # only treat qty_str as number if it really is numeric
                try:
                    qty = float(qty_str)
                except ValueError:
                    # fallback: entire string is the name
                    qty = None
                    unit = None
                    name = raw
            else:
                # fallback for 1- or 2-part entries
                qty = None
                unit = None
                name = raw

            cursor.execute(
                """
                INSERT INTO ingredients (recipe_id, name, quantity, unit)
                VALUES (%s, %s, %s, %s);
                """,
                (recipe_id, name, qty, unit)
            )

        # 3) Steps
        steps = ast.literal_eval(row["steps"])
        for i, step in enumerate(steps, start=1):
            cursor.execute(
                """
                INSERT INTO steps (recipe_id, step_number, instruction)
                VALUES (%s, %s, %s);
                """,
                (recipe_id, i, step.strip())
            )

        success += 1
    except Exception as e:
        print(f"Failed to import '{name}': {e}")
        fail += 1

# Commit & cleanup
conn.commit()
cursor.close()
conn.close()

print(f"\nImport finished: {success} recipes loaded, {fail} failed.")
