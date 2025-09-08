# accessible-cooking-assistant

Voice-driven accessible cooking assistant for visually impaired and neurodivergent users.

This project is a console app that works with both **voice** and **keyboard**.  
It lets you load recipes, move through steps hands-free, set multiple timers, do simple ingredient conversions, and even try mock “smart kitchen” controls.  
Profiles, scaling, and usage logging are included so preferences and tests can be saved.

---

## How to run

**With test data (no database needed):**
`powershell`
`$env:USE_TEST_DATA="true"`
`dotnet run --project .\src\AccessibleCookingApp\AccessibleCookingApp.csproj`

## Commands

Recipes

- list recipes        : show available recipes (test mode)
- search `<term>`      : find recipes by name (e.g., 'search pie')
- load `<name>`         : load a recipe (e.g., 'load fish pie')

Navigation

- next                : go to the next step
- previous | back     : go to the previous step
- repeat | again      : say the current step again
- print               : print all steps (announces scale if set)

Timers

- timer `<mins>`                    : start a default timer (e.g., 'timer 5')
- timer `<name>` `<mins>`             : named timer (e.g., 'timer pasta 12')
- timer `<name>` `<secs>` seconds     : seconds support (e.g., 'timer tea 30 seconds')
- set `<name>` timer for `<n>` `<unit>` : natural phrasing (e.g., 'set glaze timer for 8 minutes')
- status [name]                   : show all timers or one timer (e.g., 'status pasta')
- stop `<name>`                     : stop a named timer (e.g., 'stop oven')
- stop all                        : stop all timers

Conversions

- convert `<val>` `<from>` to `<to>`    : e.g., 'convert 240 ml to cups', 'convert 4 oz to g'
  Supported: ml, mills, cup(s), tbsp, tsp, g, oz

Scaling & profiles

- scale `<factor>`x      : set portion multiplier (e.g., 'scale 2x', 'scale 0.5x')
- units metric|imperial: set display preference for mock oven temps
- profile save `<name>`  : save current settings
- profile load `<name>`  : load saved settings

Mock kitchen controls

- preheat oven `<temp>` [C|F]  : e.g., 'preheat oven 180 c', 'set oven 350 f'
- oven off                   : mock turning oven off
- start hob `<ring>` level `<n>` : e.g., 'start hob 2 level 6'
- stop hob `<ring>`            : e.g., 'stop hob 2'

Voice & exit

- voice on | voice off  : toggle listening mode
- help                  : show this list
- end | exit            : quit
