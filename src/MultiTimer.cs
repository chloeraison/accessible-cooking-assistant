// Handles multiple timers and their associated actions.

/* **Outcome:**
This module enables:
-> Multiple timers
-> Named tracking
-> Manual checking
-> Cleanup of finished timers

/* 
**Plan:**
Purpose of this class is to manage multiple timers at once, such as:
'Set Pasta timer for 12 minutes',
'Set oven timer for 20 minutes',
'Check status of all timers',
'Clean up timers that have finished'
*/

/*Currently MultiTimer will be made up of...

A dictionary
- A Collection that maps a timer's name to the time it should finish
    -Used so that each timer has a unique name and can be checked when it ends
    
StartTimer(string name, int minutes)
- Starts or resets a time with the given name and duration
    - It does this by adding or updating an entry in the timers dictionary with the current time + minutes

CheckTimers()
- Loops through all active timers and shows how much time is left, and alerts user when timer is finished.
    - Actions by subtracting current time from each stored end time.

CleanupFinishedTimers()
- Removes any timers that have finished
    - Keeps the timers list clean and manageable


**Example flow:**
User: "Set pasta timer for 12 minutes"
StartTimer("Pasta", 12);
Later app runs CheckTimers();
    - Pasta timer shows 8 minutes left
Once it finishes, app runs CleanupFinishedTimers();
    - Pasta timer is removed from the list
(This is a simplified example, actual implementation may have more than one timer running at once)
*/


// Single timer to get timer logic working
using System;
class SingleTimer
{
    public DateTime EndTime { get; private set; }

    public void Start(int minutes)
    {
        EndTime = DateTime.Now.AddMinutes(minutes);
    }

    public TimeSpan TimeRemaining()
    {
        return EndTime - DateTime.Now;
    }

    public bool IsFinished()
    {
        return DateTime.Now >= EndTime;
    }
}