# *~*~*~*~*~*~*~*~*
# Script: import_recipes.py
# Purpose: Load and import recipe data from a CSV file in PostgreSQL database
# 
# Description:
#  1) Loads reciper data from the RAW_recipes.csv file from kaggle, named 'Food.com Recipes and Interactions'
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