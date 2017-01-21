//ScheduleTimeManager.cs
//author: Sarah Kohl
//Provides a correspondence between in-game time and real time
//and performs commonly used temporal comparisons.

using UnityEngine;
using System.Collections;

/// <summary>
/// Data class to keep keep track of a time in 24-hour format.
/// </summary>
/// <remarks>
/// Presently only internal and code acessible, though this may
/// be changed later for ease of use in the editor.
/// </remarks>
[System.Serializable]
public class ScheduleTime
{
    public int hour;
    public int minute;
    public float second;

    /// <summary>
    /// The default class constructor, starting at 0:0::0.
    /// </summary>
    public ScheduleTime()
    {
        hour = 0;
        minute = 0;
        second = 0f;
    }

    /// <summary>
    /// Copy Constructor
    /// </summary>
    /// <param name="toCopy">ScheduleTime to be copied</param>
    public ScheduleTime(ScheduleTime toCopy)
    {
        this.hour = toCopy.hour;
        this.minute = toCopy.minute;
        this.second = toCopy.second;
    }

    /// <summary>
    /// Constructor to specify a particular time.
    /// </summary>
    /// <param name="h">Hours in 24-hour format</param>
    /// <param name="m">Minutes</param>
    /// <param name="s">Seconds</param>
    public ScheduleTime(int h, int m, float s)
    {

        if (h > 23 || h < 0 || m > 59 || m < 0 || s >= 60f || s < 0f)
        {
            throw new System.Exception("Schedule Time is out of bounds.  Please provide the time in a standard 24-hour in the bounds 0:0:0 - 23:59:59.99...");
        }
        else
        {
            hour = h;
            minute = m;
            second = s;
        }
    }


    /// <summary>
    /// Exclusive greater than comparison for two times of day.
    /// </summary>
    /// <param name="lhs">Left-hand side of operator</param>
    /// <param name="rhs">Right-hand side of operator</param>
    /// <returns>True if second-sum of lhs is greater than rhs</returns>
    public static bool operator >(ScheduleTime lhs, ScheduleTime rhs)
    {
        return
        (
            (lhs.hour * 3600 + lhs.minute * 60 + lhs.second)//LH sum
            >                                               //is greater than
            (rhs.hour * 3600 + rhs.minute * 60 + rhs.second)//RH sum
        );
    }
    /// <summary>
    /// Exclusive less than comparison for two times of day.
    /// </summary>
    /// <param name="lhs">Left-hand side of operator</param>
    /// <param name="rhs">Right-hand side of operator</param>
    /// <returns>True if second-sum of lhs is less than rhs</returns>
    public static bool operator <(ScheduleTime lhs, ScheduleTime rhs)
    {
        return
        (
            (lhs.hour * 3600 + lhs.minute * 60 + lhs.second)//LH sum
            <                                               //is less than
            (rhs.hour * 3600 + rhs.minute * 60 + rhs.second)//RH sum
        );
    }

    /// <summary>
    /// Inclusive less than comparison for two times of day.
    /// </summary>
    /// <param name="lhs">Left-hand side of operator</param>
    /// <param name="rhs">Right-hand side of operator</param>
    /// <returns>True if second-sum of lhs is less than or equal to rhs</returns>
    public static bool operator <=(ScheduleTime lhs, ScheduleTime rhs)
    {
        return
        (
            (lhs.hour * 3600 + lhs.minute * 60 + lhs.second)//LH sum
            <=                                              //is less than or equal
            (rhs.hour * 3600 + rhs.minute * 60 + rhs.second)//RH sum
        );
    }


    /// <summary>
    /// Inclusive greater than comparison for two times of day.
    /// </summary>
    /// <param name="lhs">Left-hand side of operator</param>
    /// <param name="rhs">Right-hand side of operator</param>
    /// <returns>True if second-sum of lhs is greater or equal to rhs</returns>
    public static bool operator >=(ScheduleTime lhs, ScheduleTime rhs)
    {
        return
        (
            (lhs.hour * 3600 + lhs.minute * 60 + lhs.second)//LH sum
            >=                                              //is greater than or equal
            (rhs.hour * 3600 + rhs.minute * 60 + rhs.second)//RH sum
        );
    }

    /// <summary>
    /// Adds a (probably) time-scaled amount of realTime to the current in-game
    /// time.
    /// </summary>
    /// <remarks>Operator will not be overloaded to avoid mistakes.</remarks>
    /// <param name="realTime">Some amout of realTime derived from UnityEngine::Time.time</param>

    public void addRealTime(float realTime)
    {
        int thours = (int)Mathf.Floor(realTime / 3600f);
        int tminutes = (int)Mathf.Floor((realTime / 60f) % 60f);
        float tseconds = realTime % 60f;

        this.hour += thours;
        this.minute += tminutes;
        this.second += tseconds;

        this.rollOver();

    }

    /// <summary>
    /// Used internally to roll smaller intervals over to larger ones. 
    /// </summary>
    private void rollOver()
    {
        while (this.second >= 60)
        {
            this.second = this.second - 60;
            this.minute++;
        }
        while (this.minute >= 60)
        {
            this.minute = this.minute - 60;
            this.hour++;
        }

    }

    /// <summary>
    /// Can be used to print a formatted time to the console for debugging.
    /// </summary>
    /// <returns>A string with the current time.</returns>
    public override string ToString()
    {
        return this.hour + ":" + this.minute + ":" + this.second;
    }

}

/// <summary>
/// A Monobehavior that can be attached to a Controller object to keep track of
/// In-game times.
/// </summary>
public static class ScheduleTimeManager
{

    private static float timeScalar;// = 1f;                                 //this will be initialized by the global controller, where it will be a public (inspector) accessible variable
    private static float previousRealTimeElapsed;// = Time.time;             //this will be initialized by the global controller
    private static ScheduleTime currentTime;// = new ScheduleTime(0, 0, 0f); //this will be initialized by the global controller, where it is a public serializable

    /// <summary>
    /// Gets the current schedule time, and in doing so updates internal data structures
    /// that represent time BEFORE returning the in-game time.
    /// </summary>
    /// <remarks>This function technically has a nasty gotcha.  While ostensibly an accessor,
    /// it has side-effects on previousRealTimeElapsed and currentTime.
    /// </remarks>
    /// <returns>Updated member field: currentTime</returns>
    public static ScheduleTime getCurrentScheduleTime()
    {
        float dTime = Time.time - previousRealTimeElapsed;

        currentTime.addRealTime(dTime * timeScalar);

        //prepare for next call to getCurretnScheduleTime()
        previousRealTimeElapsed = Time.time;

        return currentTime;
    }

    /// <summary>
    /// Should be called only once, by the Global Controller, to initialize TimeScalar
    /// and currentTime.
    /// </summary>
    /// <param name="iTimeScalar">the TimeScalar to be initialized.</param>
    /// <param name="iStartingTime">the initial value of currentTime</param>
    public static void initializeScheduleTimeManager(float iTimeScalar, ScheduleTime iStartingTime)
    {
        timeScalar = iTimeScalar;
        currentTime = new ScheduleTime(iStartingTime);
        previousRealTimeElapsed = Time.time;

        if (timeScalar <= 0)
        {
            throw new System.Exception("TimeScalar must be greater than zero.");
        }

    }

    /// <summary>
    /// Jumps the current schedule time ahead to the specified time, or does
    /// nothing if the destination time is in the past.
    /// </summary>
    /// <param name="destinationTime">The time to jump forward to.</param>
    public static void fastForwardTo(ScheduleTime destinationTime)
    {
        if (destinationTime <= currentTime)
        {
            return;
        }
        else
        {
            currentTime = destinationTime;
        }
    }

    /// <summary>
    /// Calls ScheduleTimeManager.isTimeWithinRange on currentTime to determine
    /// if current time is in specified range.
    /// </summary>
    /// <param name="minimum">Beginning of range, exclusive</param>
    /// <param name="maximum">End of range, inclusive</param>
    /// <returns>True if within range</returns>
    public static bool isCurrentTimeWithinRange(ScheduleTime minimum, ScheduleTime maximum)
    {
        return isTimeWithinRange(currentTime, minimum, maximum);
    }

    /// <summary>
    /// Checks if first ScheduleTime parameter is in range of the latter two.
    /// </summary>
    /// <param name="time">The time to be checked.</param>
    /// <param name="minimum">Beginning of range, exclusive</param>
    /// <param name="maximum">End of range, inclusive</param>
    /// <returns></returns>
    public static bool isTimeWithinRange(ScheduleTime time, ScheduleTime minimum, ScheduleTime maximum)
    {
        if (time > minimum && time <= maximum)
            return true;
        else
            return false;
    }




}


