CREATE TABLE recipes (
    id SERIAL PRIMARY KEY,
    name TEXT NOT NULL,
    step_number INT NOT NULL,
    instruction TEXT NOT NULL
);

INSERT INTO recipes (name, step_number, instruction) VALUES
('chocolate chip cookies', 1, 'Preheat the oven to 180C.'),
('chocolate chip cookies', 2, 'Mix flour and sugar.'),
('chocolate chip cookies', 3, 'Add eggs and vanilla.'),
('chocolate chip cookies', 4, 'Stir in chocolate chips.'),
('chocolate chip cookies', 5, 'Bake for 12 minutes.');

SELECT * FROM recipes;