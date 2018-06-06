//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
    /// <summary>
    /// simple job schedule structure.
    /// </summary>

    public struct SimpleJobSchedule
    {
        #region consts
        private const int EndOfDay = 235959;
        #endregion

        #region private struct members
        private String name;
        private String description;
        private System.Int32 id;
        private System.Int32 activeEndDate;
        private System.Int32 activeEndTimeOfDay;
        private System.Int32 activeStartDate;
        private System.Int32 activeStartTimeOfDay;
        private System.Int32 frequencyInterval;
        private System.Int32 frequencyRecurrenceFactor;
        private System.Int32 frequencySubDayInterval;
        private FrequencyTypes frequencyTypes;
        private FrequencySubDayTypes frequencySubDayTypes;
        private FrequencyRelativeIntervals frequencyRelativeIntervals;
        private System.Boolean isEnabled;
        #endregion

        #region Init
        /// <summary>
        ///  set default values that the schedule dialog would show
        /// </summary>
        public void SetDefaults()
        {
            this.ActiveEndDate = ConvertDateTimeToInt(JobScheduleData.MaxAgentDateValue);
            this.ActiveEndTimeOfDay = EndOfDay;
            this.ActiveStartDate = ConvertDateTimeToInt(DateTime.Now);
            this.ActiveStartTimeOfDay = 0;
            this.FrequencyInterval = 0;
            this.FrequencyRecurrenceFactor = 1;
            this.frequencyRelativeIntervals = FrequencyRelativeIntervals.First;
            this.FrequencySubDayInterval = 0;
            this.frequencySubDayTypes = FrequencySubDayTypes.Unknown;
            this.frequencyTypes = FrequencyTypes.Weekly;
            this.isEnabled = true;
            this.ID = -1;
        }
        #endregion

        #region public conversion helpers
        /// <summary>
        /// Convert SqlAgent date format to Urt DateTime struct.
        /// Also validates against the culture range.
        /// </summary>
        /// <param name="source">Agent date of the form yyyymmdd</param>
        /// <param name="minDate"></param>
        /// <param name="maxDate"></param>
        /// <returns>DateTime representation of the date</returns>
        private static DateTime ConvertIntToDateLocalized(int source)
        {
            try
            {
                return ConvertIntToDateTime(source);
            }
            catch (ArgumentException)
            {}

            // If there is an exception, can only AFTER DateTime.MaxDate.
            // This is because some calendars have a lower MaxDate.
            return JobScheduleData.MaxAgentDateValue;

        }
        
        /// <summary>
        /// Convert SqlAgent date format to Urt DateTime struct
        /// </summary>
        /// <param name="source">Agent date of the form yyyymmdd</param>
        /// <returns>DateTime representation of the date</returns>
        public static DateTime ConvertIntToDateTime(int source)
        {
            return new DateTime(source / 10000
                                , (source / 100) % 100
                                , source % 100);
        }
        /// <summary>
        /// Convert DateTime to a SqlAgent date format
        /// </summary>
        /// <param name="source">source date</param>
        /// <returns>int of the form yyyymmdd</returns>
        public static int ConvertDateTimeToInt(DateTime source)
        {
            if (source > JobScheduleData.MaxAgentDateValue)
            {
                source = JobScheduleData.MaxAgentDateValue;
            }
            return source.Year * 10000
                + source.Month * 100
                + source.Day;
        }
        /// <summary>
        /// convert an agent time to a timespan
        /// </summary>
        /// <param name="source">timespan in the form hhmmss</param>
        /// <returns>TimeSpan representing the agent time</returns>
        public static TimeSpan ConvertIntToTimeSpan(int source)
        {
            return new TimeSpan(source / 10000
                                , (source / 100) % 100
                                , source % 100);
        }
        /// <summary>
        /// Convert a TimeSpan to an agent compatible int
        /// </summary>
        /// <param name="source">Timespan</param>
        /// <returns>int in the form hhmmss</returns>
        public static int ConvertTimeSpanToInt(TimeSpan source)
        {
            if (source > JobScheduleData.MaxAgentTimeValue)
            {
                source = JobScheduleData.MaxAgentTimeValue;
            }
            return source.Hours * 10000
                + source.Minutes * 100
                + source.Seconds;
        }
        /// <summary>
        /// Create a new JobScheduleData based on the current structure
        /// </summary>
        /// <returns>JobScheduleData object</returns>
        public JobScheduleData ToJobScheduleData()
        {
            JobScheduleData data = new JobScheduleData();
            data.Name = this.Name;
            data.Enabled = this.IsEnabled;
            data.ActiveStartDate = SimpleJobSchedule.ConvertIntToDateLocalized(this.ActiveStartDate);
            data.ActiveStartTime = SimpleJobSchedule.ConvertIntToTimeSpan(this.ActiveStartTimeOfDay);
            data.ActiveEndDate = SimpleJobSchedule.ConvertIntToDateLocalized(this.ActiveEndDate);
            data.ActiveEndTime = SimpleJobSchedule.ConvertIntToTimeSpan(this.ActiveEndTimeOfDay);
            data.FrequencyTypes = this.FrequencyTypes;
            data.FrequencyInterval = this.FrequencyInterval;
            data.FrequencyRecurranceFactor = this.FrequencyRecurrenceFactor;
            data.FrequencyRelativeIntervals = this.FrequencyRelativeIntervals;
            data.FrequencySubDayInterval = this.FrequencySubDayInterval;
            data.FrequencySubDayTypes = this.FrequencySubDayTypes;

            return data;
        }
        /// <summary>
        /// create a new SimpleJobSchedule structure based upon a JobScheduleData object.
        /// </summary>
        /// <param name="source">JobScheduleData object</param>
        /// <returns>new SimpleJobSchedule</returns>
        public static SimpleJobSchedule FromJobScheduleData(JobScheduleData source)
        {
            SimpleJobSchedule schedule = new SimpleJobSchedule();

            schedule.Name = source.Name;
            schedule.ID = source.ID;
            schedule.IsEnabled = source.Enabled;
            schedule.ActiveStartDate = SimpleJobSchedule.ConvertDateTimeToInt(source.ActiveStartDate);
            schedule.ActiveStartTimeOfDay = SimpleJobSchedule.ConvertTimeSpanToInt(source.ActiveStartTime);
            schedule.ActiveEndDate = SimpleJobSchedule.ConvertDateTimeToInt(source.ActiveEndDate);
            schedule.ActiveEndTimeOfDay = SimpleJobSchedule.ConvertTimeSpanToInt(source.ActiveEndTime);
            schedule.FrequencyTypes = source.FrequencyTypes;
            schedule.FrequencyInterval = source.FrequencyInterval;
            schedule.FrequencyRecurrenceFactor = source.FrequencyRecurranceFactor;
            schedule.FrequencyRelativeIntervals = source.FrequencyRelativeIntervals;
            schedule.FrequencySubDayInterval = source.FrequencySubDayInterval;
            schedule.FrequencySubDayTypes = source.FrequencySubDayTypes;

            schedule.Description = schedule.ComputeDescription();
            return schedule;
        }
        
        #endregion

        #region public properties for use by ExpandFormatString

        /// Called indirectly by ExpandFormatString through a TypeDescriptor lookup
        public int Frequency
        {
            get
            {
                return this.frequencyTypes == FrequencyTypes.Daily ? this.FrequencyInterval : this.FrequencyRecurrenceFactor;
            }
        }

        /// Called indirectly by ExpandFormatString through a TypeDescriptor lookup
        public string StartDate
        {
            get
            {
                return ConvertIntToDateLocalized(this.ActiveStartDate).ToString("d", CultureInfo.CurrentCulture);
            }
        }

        /// Called indirectly by ExpandFormatString through a TypeDescriptor lookup
        public string StartTimeOfDay
        {
            get
            {
                return (DateTime.MinValue + ConvertIntToTimeSpan(this.ActiveStartTimeOfDay)).ToLongTimeString();
            }
        }

        /// Called indirectly by ExpandFormatString through a TypeDescriptor lookup
        public string EndTimeOfDay
        {
            get
            {
                return (DateTime.MinValue + ConvertIntToTimeSpan(this.ActiveEndTimeOfDay)).ToLongTimeString();
            }
        }

        /// Called indirectly by ExpandFormatString through a TypeDescriptor lookup
        public int TimeInterval
        {
            get
            {
                return this.FrequencySubDayInterval;
            }
        }


        /// Called indirectly by ExpandFormatString through a TypeDescriptor lookup
        public int DayOfMonth
        {
            get
            {
                return this.FrequencyInterval;
            }
        }

        /// Called indirectly by ExpandFormatString through a TypeDescriptor lookup
        public string WeekOfMonth
        {
            get
            {
                // STrace.Assert(this.FrequencyTypes == FrequencyTypes.MonthlyRelative);
                // switch (this.FrequencyRelativeIntervals)
                // {
                //     case FrequencyRelativeIntervals.First:
                //         return SR.First;
                        
                //     case FrequencyRelativeIntervals.Second:
                //         return SR.Second;
                        
                //     case FrequencyRelativeIntervals.Third:
                //         return SR.Third;
                        
                //     case FrequencyRelativeIntervals.Fourth:
                //         return SR.Fourth;
                        
                //     case FrequencyRelativeIntervals.Last:
                //         return SR.Last;
                        
                //     default:
                //         Debug.Assert(false, "Unknown FrequencyRelativeIntervals");
                //         throw new InvalidOperationException();
                // }
                return "First";
            }
        }
        
        /// Called indirectly by ExpandFormatString through a TypeDescriptor lookup
        public string DayOfWeekList
        {
            get
            {
                if (this.FrequencyInterval == 0)
                {
                    return String.Empty;
                }

                StringBuilder daysOfWeek = new StringBuilder();
                // Start matching with Monday. GetLocalizedDaysOfWeek() must start with Monday too.
                WeekDays dayOfWeek = WeekDays.Monday;
                foreach (string localizedDayOfWeek in GetLocalizedDaysOfWeek())
                {
                    if ((this.FrequencyInterval & (int)dayOfWeek) != 0)
                    {
                        if (daysOfWeek.Length > 0)
                        {
                            daysOfWeek.Append("SR.DescriptionSeparatorBetweenWeekDays");
                        }
                        daysOfWeek.Append(localizedDayOfWeek);
                    }

                    // There's no easy way to advance to the next enum value, so since we know
                    // it's a bitfield mask we do a left shift ourselves.
                    int nextDay = ((int)dayOfWeek) << 1;
                    dayOfWeek = (WeekDays)nextDay;
                    if (dayOfWeek > WeekDays.Saturday)
                    {
                        // Since we started with Monday but the enum starts with Sunday, go to
                        // Sunday after Saturday
                        dayOfWeek = WeekDays.Sunday;
                    }
                }

                return daysOfWeek.ToString();
            }
        }

        /// Called indirectly by ExpandFormatString through a TypeDescriptor lookup
        public string TimeIntervalUnit
        {
            get
            {
                switch (this.FrequencySubDayTypes)
                {
                    case FrequencySubDayTypes.Hour:
                        return "SR.TimeFrequencyHour1N";
                    case FrequencySubDayTypes.Minute:
                        return "SR.TimeFrequencyMinute1N";
                    case FrequencySubDayTypes.Second:
                        return "SR.TimeFrequencySecond1N";
                    default:
                        Debug.Assert(false, "Unknown frequency sub day type");
                        throw new InvalidOperationException();
                }
            }
        }

        /// Called indirectly by ExpandFormatString through a TypeDescriptor lookup
        public string ScheduleRecurrenceAndTimes
        {
            get
            {
                string format = String.Empty;

                switch (this.FrequencyTypes)
                {
                    //--------------------- DAILY
                    case FrequencyTypes.Daily:
                        if (this.FrequencyInterval == 1)
                        {
                            format = this.frequencySubDayTypes == FrequencySubDayTypes.Once
                                ? "SR.RecurringEveryDayAtTime" : "SR.RecurringEveryDayBetweenTimes";
                        }
                        else
                        {
                            format = this.frequencySubDayTypes == FrequencySubDayTypes.Once
                                ? "SR.RecurringDailyAtTime" : "SR.RecurringDailyBetweenTimes";
                        }
                        break;

                    //---------------------- WEEKLY
                    case FrequencyTypes.Weekly:
                        if (this.FrequencyRecurrenceFactor == 1)
                        {
                            if (this.FrequencyInterval == 0)
                            {
                                format = this.frequencySubDayTypes == FrequencySubDayTypes.Once
                                    ? "SR.RecurringEveryWeekAtTime" : "SR.RecurringEveryWeekBetweenTimes";
                            }
                            else
                            {
                                format = this.frequencySubDayTypes == FrequencySubDayTypes.Once
                                    ? "SR.RecurringEveryWeekOnDaysOfWeekAtTime" : "SR.RecurringEveryWeekOnDaysOfWeekBetweenTimes";
                            }
                        }
                        else
                        {
                            if (this.FrequencyInterval == 0)
                            {
                                format = this.frequencySubDayTypes == FrequencySubDayTypes.Once
                                    ? "SR.RecurringWeeklyAtTime" : "SR.RecurringWeeklyBetweenTimes";
                            }
                            else
                            {
                                format = this.frequencySubDayTypes == FrequencySubDayTypes.Once
                                    ? "SR.RecurringWeeklyOnDaysOfWeekAtTime" : "SR.RecurringWeeklyOnDaysOfWeekBetweenTimes";
                            }
                        }
                        break;

                    //---------------------- MONTHLY
                    case FrequencyTypes.Monthly:
                        if (this.FrequencyRecurrenceFactor == 1)
                        {
                            format = this.frequencySubDayTypes == FrequencySubDayTypes.Once
                                ? "SR.RecurringEveryMonthOnDayAtTime" : "SR.RecurringEveryMonthOnDayBetweenTimes";
                        }
                        else
                        {
                            format = this.frequencySubDayTypes == FrequencySubDayTypes.Once
                                ? "SR.RecurringMonthlyAtTime" : "SR.RecurringMonthlyBetweenTimes";
                        }
                        break;

                    //---------------------- MONTHLY RELATIVE
                    case FrequencyTypes.MonthlyRelative:
                        format = this.frequencySubDayTypes == FrequencySubDayTypes.Once
                            ? "SR.RecurringRelativeMonthlyAtTime" : "SR.RecurringRelativeMonthlyBetweenTimes";
                        break;

                    default:
                        Debug.Assert(false, "Unsupported recurrence type");
                        throw new InvalidOperationException();
                }

                return ExpandFormatString(format);
            }
        }

        /// Called indirectly by ExpandFormatString through a TypeDescriptor lookup
        public string ScheduleDates
        {
            get
            {
                string startDate = ConvertIntToDateLocalized(this.ActiveStartDate).ToString("d", CultureInfo.CurrentCulture);
                string endDate = ConvertIntToDateLocalized(this.ActiveEndDate).ToString("d", CultureInfo.CurrentCulture);

                if (endDate != JobScheduleData.MaxAgentDateValue.ToShortDateString())
                {
                    return "SR.ScheduleBetweenDates(startDate, endDate)";
                }
                else
                {
                    return "SR.ScheduleStartingWithDate(startDate)";
                }
            }
        }

        /// Called indirectly by ExpandFormatString through a TypeDescriptor lookup
        public string DayOfWeek
        {
            get
            {            
                MonthlyRelativeWeekDays relativeDays = (MonthlyRelativeWeekDays) this.FrequencyInterval;
                
                switch (relativeDays)
                {
                    case MonthlyRelativeWeekDays.Sunday:    return "SR.Sunday";
                    case MonthlyRelativeWeekDays.Monday:    return "SR.Monday";
                    case MonthlyRelativeWeekDays.Tuesday:   return "SR.Tuesday";
                    case MonthlyRelativeWeekDays.Wednesday: return "SR.Wednesday";
                    case MonthlyRelativeWeekDays.Thursday:  return "SR.Thursday";
                    case MonthlyRelativeWeekDays.Friday:    return "SR.Friday";
                    case MonthlyRelativeWeekDays.Saturday:  return "SR.Saturday";
                    case MonthlyRelativeWeekDays.EveryDay:  return "SR.Day";
                    case MonthlyRelativeWeekDays.WeekDays:  return "SR.Weekday";
                    case MonthlyRelativeWeekDays.WeekEnds:  return "SR.Weekend";
                    default:
                        Debug.Assert(false, "Unknown category of day");
                        throw new InvalidOperationException();
                }
            }
        }
        
        #endregion
        
        #region public convert ToString()
        /// <summary>
        /// returns a localized string description (without needing to instantiate UI for that)
        /// 
        /// output is similar with the one displayed by the dialog
        /// it is computed using following logic
        /// 
        /// case:
        /// on agent startup:
        ///     static string
        /// on idle:
        ///     static string
        /// recurring:
        ///     activePattern (day/week/month) description +
        ///     nestedReccurence description (occurs once at/occcurs every...)
        ///     duration (starttime/endtime)
        ///     
        ///                [Flags] - src\shared\Smo\Enumerator\sql\src\enumstructs.cs
        ///                public enum FrequencyTypes
        ///                AutoStart = 64, // Scheduled activity is started when SQL Server Agent service starts. 
        ///                Daily = 4, // Schedule is evaluated daily. 
        ///                Monthly = 16, // Schedule is evaluated monthly. 
        ///                MonthlyRelative = 32, // Schedule is evaluated relative to a part of a month, such as the second week. 
        ///                OneTime = 1, // Scheduled activity will occur once at a scheduled time or event. 
        ///                OnIdle = 128, // SQL Server Agent service will schedule the activity for any time during which the processor is idle. 
        ///                Unknown = 0, // No schedule frequency, or frequency not applicable. 
        ///                Weekly = 8 // Schedule is evaluated weekly. 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string description = this.ComputeDescription();
            return description;
        }
        #endregion

        #region public properties
        public System.Int32 ActiveEndDate
        {
            get { return activeEndDate; }
            set { activeEndDate = value; }
        }

        public System.Int32 ActiveEndTimeOfDay
        {
            get { return activeEndTimeOfDay; }
            set { activeEndTimeOfDay = value; }
        }

        public System.Int32 ActiveStartDate
        {
            get { return activeStartDate; }
            set { activeStartDate = value; }
        }

        public System.Int32 ActiveStartTimeOfDay
        {
            get { return activeStartTimeOfDay; }
            set { activeStartTimeOfDay = value; }
        }

        public System.Int32 FrequencyInterval
        {
            get { return frequencyInterval; }
            set { frequencyInterval = value; }
        }

        public System.Int32 FrequencyRecurrenceFactor
        {
            get { return frequencyRecurrenceFactor; }
            set { frequencyRecurrenceFactor = value; }
        }

        public System.Int32 FrequencySubDayInterval
        {
            get { return frequencySubDayInterval; }
            set { frequencySubDayInterval = value; }
        }

        public FrequencyTypes FrequencyTypes
        {
            get { return frequencyTypes; }
            set { frequencyTypes = value; }
        }

        public FrequencySubDayTypes FrequencySubDayTypes
        {
            get { return frequencySubDayTypes; }
            set { frequencySubDayTypes = value; }
        }

        public FrequencyRelativeIntervals FrequencyRelativeIntervals
        {
            get { return frequencyRelativeIntervals; }
            set { frequencyRelativeIntervals = value; }
        }

        public string Name
        {
            get
            {
                return this.name;
            }
            set
            {
                this.name = value;
            }
        }

        public string Description
        {
            get
            {
                return this.description;
            }
            set
            {
                this.description = value;
            }
        }

        public System.Int32 ID
        {
            get { return id; }
            set { id = value; }
        }

        public bool IsEnabled
        {
            get
            {
                return this.isEnabled;
            }
            set
            {
                this.isEnabled = value;
            }
        }

        #endregion
        

        #region private implementation - compute description
        /// <summary>
        /// computes description for a this schedule
        /// 
        /// for info about the meaning of all those parameters see MSDN doc for sp_add_job_schedule
        /// e.g. http://msdn.microsoft.com/library/en-us/tsqlref/ts_sp_adda_6ijp.asp?frame=true
        /// </summary>
        /// <returns>localized description</returns>
        private string ComputeDescription()
        {
            try
            {
                switch (this.FrequencyTypes)
                {
                    //--------------------- AUTOSTART 
                    case FrequencyTypes.AutoStart:
                        return "SR.AutoStartSchedule"; // full static description
                    //--------------------- ONIDLE
                    case FrequencyTypes.OnIdle:
                        return "SR.CPUIdleSchedule"; // full static description
                    //--------------------- DAILY
                    case FrequencyTypes.Daily:
                    //---------------------- WEEKLY
                    case FrequencyTypes.Weekly:
                    //---------------------- MONTHLY
                    case FrequencyTypes.Monthly:
                    //---------------------- MONTHLY RELATIVE
                    case FrequencyTypes.MonthlyRelative:
                        return ExpandFormatString("SR.RecurrentScheduleDescription");
                    //---------------------- ONE TIME
                    case FrequencyTypes.OneTime:
                        return ExpandFormatString("SR.OneTimeScheduleDescription");
                    //---------------------- UNKNOWN
                    case 0:
                        Debug.Assert(false, "frequency type is .Unknown");
                        throw new InvalidOperationException();
                    //---------------------- UNHANDLED
                    default:
                        Debug.Assert(false, "WARNING: unhandled frequency type");
                        throw new InvalidOperationException();
                }
            }
            catch (InvalidOperationException)
            {
                return "SR.UnknownScheduleType";
            }
        }

        /// <summary>
        /// Substitutes property placeholders in a format string with property values.
        /// Example of format string:
        /// Occurs every {WeekOfMonth} {DaysOfWeekList} of every {FrequencyRecurrenceFactor} month(s) every {FrequencySubDayInterval} {FrequencySubDayIntervalUnit} between {ActiveStartTimeOfDay} and {ActiveEndTimeOfDay}.
        /// </summary>
        /// <param name="format">Format string.</param>
        /// <returns></returns>
        string ExpandFormatString(string format)
        {
            StringBuilder stringBuilder = new StringBuilder();
            int lastIndex = 0;

            MatchCollection matches = Regex.Matches(format, @"\{(?<property>\w+)\}");

            if (matches.Count > 0)
            {
                PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(this);

                foreach (Match match in matches)
                {
                    string propertyName = match.Groups["property"].Value;
                    PropertyDescriptor property = properties[propertyName];                 
                    if (property != null)
                    {
                        object propertyValue = property.GetValue(this);
                        propertyValue = propertyValue != null ? propertyValue.ToString() : String.Empty;

                        stringBuilder.Append(format.Substring(lastIndex, match.Index - lastIndex));
                        stringBuilder.Append(propertyValue as string);
                        lastIndex = match.Index + match.Length;
                    }
                }
            }

            stringBuilder.Append(format.Substring(lastIndex));
            return stringBuilder.ToString();
        }

        private static string[] GetLocalizedDaysOfWeek()
        {
            return new string[] { "SR.Monday", "SR.Tuesday", "SR.Wednesday", "SR.Thursday", "SR.Friday", "SR.Saturday", "SR.Sunday" };
        }

        #endregion
    }

#if !DEBUG && !EXPOSE_MANAGED_INTERNALS
	[System.Security.Permissions.StrongNameIdentityPermissionAttribute(
		System.Security.Permissions.SecurityAction.LinkDemand, 
		PublicKey=Microsoft.SqlServer.Management.SqlMgmt.CodeSigning.PublicKeyConstants.PublicKey)]
#endif
    public class JobScheduleData
    {
        #region data members
        private string currentName;
        private string originalName;
        private bool enabled;
        bool alreadyCreated;
        Urn urn;
        private DateTime startDate;
        private TimeSpan startTime;
        private DateTime endDate;
        private TimeSpan endTime;
        private FrequencyTypes frequencyType;
        private int frequencyInterval;
        private int frequencyRecurranceFactor;
        private FrequencyRelativeIntervals frequencyRelativeInterval;
        private int frequencySubDayInterval;
        private FrequencySubDayTypes frequencySubDayTypes;
        private Job parentJob = null;
        private JobSchedule source = null;
        private JobServer parentJobServer = null; // true only if we create shared schedule
        private int id = -1;
        private bool isReadOnly = false;
        private bool allowEnableDisable = true;
        #endregion

        #region construction
        /// <summary>
        /// Initialize a new JobScheduleData object that is empty.
        /// </summary>
        public JobScheduleData()
        {
            this.parentJob = null;
            SetDefaults();
        }

        /// <summary>
        /// Initialize a new JobScheduleData object with a parent Job.
        /// </summary>
        /// <param name="parentJob">Job that will own this schedule.</param>
        public JobScheduleData(Job parentJob)
        {
            if (parentJob == null)
            {
                throw new ArgumentNullException("parentJob");
            }
            this.parentJob = parentJob;
            SetDefaults();
        }
        /// <summary>
        /// Initializes a new JobScheduleData object that represents an existing Schedule.
        /// </summary>
        /// <param name="source">The SMO schedule that this object will represent.</param>
        public JobScheduleData(JobSchedule source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            this.source = source;
            LoadData(source);
        }

        /// <summary>
        /// Initializes a new JobScheduleData object that represents an existing Schedule.
        /// </summary>
        /// <param name="source">The SMO schedule that this object will represent.</param>
        public JobScheduleData(JobSchedule source, bool isReadOnly, bool allowEnableDisable)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            this.source = source;
            this.isReadOnly = isReadOnly;
            this.allowEnableDisable = allowEnableDisable;
            LoadData(source);
        }


        #endregion

        #region public members
        public int ID
        {
            get
            {
                return this.id;
            }
            set
            {
                this.id = value;
            }
        }
        public string Name
        {
            get
            {
                return this.currentName;
            }
            set
            {
                this.currentName = value;
            }
        }
        public bool Enabled
        {
            get
            {
                return this.enabled;
            }
            set
            {
                this.enabled = value;
            }
        }
        public bool AllowEnableDisable
        {
            get
            {
                return this.allowEnableDisable;
            }
        }
        public string Description
        {
            get
            {
                return this.ToString();
            }
        }
        public bool Created
        {
            get
            {
                return this.alreadyCreated;
            }
            set
            {
                this.alreadyCreated = value;
            }
        }
        public DateTime ActiveStartDate
        {
            get
            {
                return this.startDate;
            }
            set
            {
                this.startDate = value;
            }
        }
        public TimeSpan ActiveStartTime
        {
            get
            {
                return this.startTime;
            }
            set
            {
                this.startTime = value;
            }
        }
        public DateTime ActiveEndDate
        {
            get
            {
                return this.endDate;
            }
            set
            {
                this.endDate = (value > JobScheduleData.MaxAgentDateValue)
                   ? JobScheduleData.MaxAgentDateValue
                   : value;
            }
        }
        public TimeSpan ActiveEndTime
        {
            get
            {
                return this.endTime;
            }
            set
            {
                this.endTime = (value > JobScheduleData.MaxAgentTimeValue)
                   ? JobScheduleData.MaxAgentTimeValue
                   : value;
            }
        }
        public bool HasEndDate
        {
            get
            {
                return this.endDate < JobScheduleData.MaxAgentDateValue;
            }
        }
        public bool HasEndTime
        {
            get
            {
                return this.endTime < JobScheduleData.MaxAgentTimeValue;
            }
        }
        public FrequencyTypes FrequencyTypes
        {
            get
            {
                return this.frequencyType;
            }
            set
            {
                this.frequencyType = value;
            }
        }
        public int FrequencyInterval
        {
            get
            {
                return this.frequencyInterval;
            }
            set
            {
                this.frequencyInterval = value;
            }
        }
        public int FrequencyRecurranceFactor
        {
            get
            {
                return this.frequencyRecurranceFactor;
            }
            set
            {
                this.frequencyRecurranceFactor = value;
            }
        }
        public FrequencyRelativeIntervals FrequencyRelativeIntervals
        {
            get
            {
                return this.frequencyRelativeInterval;
            }
            set
            {
                this.frequencyRelativeInterval = value;
            }
        }
        public int FrequencySubDayInterval
        {
            get
            {
                return this.frequencySubDayInterval;
            }
            set
            {
                this.frequencySubDayInterval = value;
            }
        }
        public FrequencySubDayTypes FrequencySubDayTypes
        {
            get
            {
                return this.frequencySubDayTypes;
            }
            set
            {
                this.frequencySubDayTypes = value;
            }
        }
        public JobSchedule SourceSchedule
        {
            get
            {
                return this.source;
            }
        }
        public bool IsReadOnly
        {
            get { return isReadOnly; }
            set { isReadOnly = value; }
        }

        #endregion

        #region public methods
        /// <summary>
        /// Save any changes
        /// </summary>
        /// <returns>True if any changes were commited</returns>
        public bool ApplyChanges()
        {
            bool changesMade = UpdateSourceSchedule();

            // save the changes.
            if (this.alreadyCreated)
            {
                source.Alter();
            }
            else
            {
                source.Create();
                // retrieving source.ID after creation would throw if the
                // server was in CaptureSql mode. This is because the schedule
                // id is not generated while capturing sql. Thus, we only query
                // id and set the created flag to true only when the smo object
                // is actually created and not scripted.
                Microsoft.SqlServer.Management.Smo.Server svr = null;
                if (this.parentJob != null && this.parentJob.Parent != null && this.parentJob.Parent.Parent != null)
                {
                    svr = this.parentJob.Parent.Parent as Microsoft.SqlServer.Management.Smo.Server;
                }
                if (svr == null || SqlExecutionModes.CaptureSql != (SqlExecutionModes.CaptureSql & svr.ConnectionContext.SqlExecutionModes))
                {
                    this.id = source.ID;

                    this.Created = true;
                }
            }
            return changesMade;
        }

        /// <summary>
        /// Utility function that creates or updates the internal
        /// JobSchedule. The JobSchedule is not Created or Altered,
        /// however, so these changes are not written to SQL.
        /// </summary>
        public bool UpdateSourceSchedule()
        {
            bool changesMade = false;
            // cannot apply changes if we were created with a struct.
            if (this.source == null && this.parentJob == null && this.parentJobServer == null)
            {
                return false;
            }

            if (!this.alreadyCreated)
            {
                // creating a new job. setup the name. Create the object.
                this.originalName = this.currentName;

                if (this.IsSharedSchedule)
                {
                    this.source = new JobSchedule(this.parentJobServer, this.Name); // ID for this.source is created by agent, we don't have to specify it
                }
                else // job schedule
                {
                    this.source = new JobSchedule(this.parentJob, this.Name); // ID for this.source is created by agent, we don't have to specify it
                }
                changesMade = true;
            }
            else if (this.originalName != this.currentName)
            {
                // must explicitly rename an object.
                this.source.Rename(currentName);
                this.originalName = this.currentName;
                changesMade = true;
            }

            if (!this.alreadyCreated || this.enabled != source.IsEnabled)
            {
                source.IsEnabled = this.enabled;
                changesMade = true;
            }
            if (!this.alreadyCreated || this.frequencyType != source.FrequencyTypes)
            {
                source.FrequencyTypes = this.frequencyType;
                changesMade = true;
            }
            // use properties for the date and time properties as the agent accepts
            // different max dates/times as the ndp. These manage this.
            if (!this.alreadyCreated || this.startDate != source.ActiveStartDate)
            {
                source.ActiveStartDate = this.ActiveStartDate;
                changesMade = true;
            }
            if (!this.alreadyCreated || this.startTime != source.ActiveStartTimeOfDay)
            {
                source.ActiveStartTimeOfDay = this.ActiveStartTime;
                changesMade = true;
            }
            if (!this.alreadyCreated || this.endDate != source.ActiveEndDate)
            {
                source.ActiveEndDate = this.ActiveEndDate;
                changesMade = true;
            }
            if (!this.alreadyCreated || this.endTime != source.ActiveEndTimeOfDay)
            {
                source.ActiveEndTimeOfDay = this.ActiveEndTime;
                changesMade = true;
            }
            if (!this.alreadyCreated || this.frequencyInterval != source.FrequencyInterval)
            {
                source.FrequencyInterval = this.frequencyInterval;
                changesMade = true;
            }
            if (!this.alreadyCreated || this.frequencyRecurranceFactor != source.FrequencyRecurrenceFactor)
            {
                source.FrequencyRecurrenceFactor = this.frequencyRecurranceFactor;
                changesMade = true;
            }
            if (!this.alreadyCreated || this.frequencyRelativeInterval != source.FrequencyRelativeIntervals)
            {
                source.FrequencyRelativeIntervals = this.frequencyRelativeInterval;
                changesMade = true;
            }
            if (!this.alreadyCreated || this.frequencySubDayInterval != source.FrequencySubDayInterval)
            {
                source.FrequencySubDayInterval = this.frequencySubDayInterval;
                changesMade = true;
            }
            if (!this.alreadyCreated || this.frequencySubDayTypes != source.FrequencySubDayTypes)
            {
                source.FrequencySubDayTypes = this.frequencySubDayTypes;
                changesMade = true;
            }
            return changesMade;
        }

        /// <summary>
        /// Validate job schedule data object
        /// </summary>
        /// <param name="version"></param>
        /// <param name="schedules"></param>
        public void Validate(System.Version version, ArrayList schedules)
        {
            DateTime minStartDate = new DateTime(1990, 1, 1);
            ArrayList jobSchedules;
            StringBuilder sbError = new StringBuilder();

            if (schedules != null)
            {
                jobSchedules = schedules;
            }
            else
            {
                jobSchedules = null;
            }

            if (jobSchedules != null && version.Major < 9)
            {
                ///Check to see if a duplicate job schedule name has been entered.  This condition is permissable
                ///in SQL 9, but not in previous versions.
                for (int index = 0; index < jobSchedules.Count; index++)
                {
                    //If the schedule name matches an existing job, throw an error and ask user to enter another
                    //name.
                    if (((JobScheduleData)jobSchedules[index]).Name == this.Name &&
                        this.currentName != this.originalName)
                    {
                        sbError.Append("SRError.ScheduleNameAlreadyExists(this.Name)" + "\n");
                        break;
                    }
                }
            }

            // weekly schdule - ensure that the start date is less than the end date
            if (this.ActiveStartDate > this.ActiveEndDate)
            {
                sbError.Append("SRError.StartDateGreaterThanEndDate" + "\n");
            }

            //One Time events validations
            if (this.FrequencyTypes == FrequencyTypes.OneTime)
            {
                //Check to make sure that the start time is greater than the baseline
                //date of 01/01/1990
                if (this.ActiveStartDate < minStartDate)
                {
                    sbError.Append("SRError.InvalidStartDate" + "\n");
                }
            }


            //Recurring schdule.  Start time must be less than the end time.
            if (this.FrequencyTypes != FrequencyTypes.OneTime &&
                this.FrequencyTypes != FrequencyTypes.OnIdle)
            {
                //Check to make sure that the start time is greater than the baseline
                //date of 01/01/1990
                if (this.ActiveStartDate < minStartDate)
                {
                    sbError.Append("SRError.InvalidStartDate" + "\n");
                }


                //Check to ensure that the StartTime != to the EndTime
                if (this.ActiveStartTime == this.ActiveEndTime)
                {
                    sbError.Append("SRError.EndTimeEqualToStartTime" + "\n");

                }

            }

            // weekly schedule - at least one day should be selected
            if (this.FrequencyTypes == FrequencyTypes.Weekly)
            {
                if (this.FrequencyInterval == 0)
                {
                    sbError.Append("SRError.InvalidWeeklySchedule" + "\n");
                }
            }

            // $FUTURE add extra checks in future - e.g. 147675 - starttime/endtime startdate/enddate and thier format

            // return error
            if (sbError.ToString().Length > 0)
            {
                throw new ApplicationException(sbError.ToString());
            }
            return;
        }

        /// <summary>
        /// Overloaded method to use in cases where no data container context is available.
        /// </summary>
        public void Validate()
        {
            this.Validate(null, null);
        }

        #endregion

        /// <summary>
        /// Delete the schedule.
        /// </summary>
        public void Delete()
        {
            // need to be created
            if (this.source != null && this.alreadyCreated)
            {
                this.source.Drop();
                this.source = null;
                this.alreadyCreated = false;
            }
        }
        /// <summary>
        /// Provide context to create a new schedule.
        /// </summary>
        /// <param name="job">Job that will own this schedule.</param>
        public void SetJob(Job job)
        {
            this.parentJob = job;
        }
        /// <summary>
        /// Provide context to edit an existing schedule.
        /// </summary>
        /// <param name="schedule">Schedule this object represents.</param>
        public void SetJobSchedule(JobSchedule schedule)
        {
            this.source = schedule;
        }
        public override string ToString()
        {
            return this.GetSimpleJobDescription();
        }

        /// <summary>
        /// marks job schedule to be created as a parentless shared schedule (no job associated with it) 
        /// </summary>
        /// <param name="sharedMode">true if you want to create this shared schedule without a parent (yukon only)</param>
        public void SetJobServer(JobServer parentServer)
        {
            this.parentJobServer = parentServer;
        }

        public bool IsSharedSchedule
        {
            get
            {
                return (this.parentJobServer != null);
            }
        }



        #region load data
        /// <summary>
        /// setup internal members based upon a JobSchedule object.
        /// </summary>
        /// <param name="source"></param>
        private void LoadData(JobSchedule source)
        {
            currentName = originalName = source.Name;
            this.urn = source.Urn;
            this.alreadyCreated = true;

            this.enabled = source.IsEnabled;

            this.frequencyType = source.FrequencyTypes;

            this.startDate = source.ActiveStartDate;
            this.startTime = source.ActiveStartTimeOfDay;
            this.endDate = source.ActiveEndDate;
            this.endTime = source.ActiveEndTimeOfDay;

            this.frequencyInterval = source.FrequencyInterval;

            this.frequencyRecurranceFactor = source.FrequencyRecurrenceFactor;
            this.frequencyRelativeInterval = source.FrequencyRelativeIntervals;

            this.frequencySubDayInterval = source.FrequencySubDayInterval;
            this.frequencySubDayTypes = source.FrequencySubDayTypes;

            // If this JobSchedule object hasn't been Created yet,
            // then accessing the ID will fail.
            try
            {
                this.id = source.ID;
            }
            catch (Microsoft.SqlServer.Management.Smo.PropertyNotSetException)
            {
                this.alreadyCreated = false;
            }
        }
        /// <summary>
        /// set defaults assuming no parent.
        /// </summary>
        private void SetDefaults()
        {
            this.alreadyCreated = false;
            currentName = originalName = String.Empty;
            this.enabled = true;

            this.frequencyType = FrequencyTypes.Weekly;  //SQL2K default value 

            this.startDate = DateTime.Now;
            this.startTime = TimeSpan.Zero;

            this.endDate = JobScheduleData.MaxAgentDateValue;
            this.endTime = JobScheduleData.MaxAgentTimeValue;

            this.frequencyInterval = 1; // sunday, SQL2K default value

            this.frequencyRecurranceFactor = 1;

            this.frequencyRelativeInterval = 0;

            this.frequencySubDayInterval = 0;
            this.frequencySubDayTypes = 0;

            this.id = -1;
        }
        #endregion

        #region To String
        private string GetSimpleJobDescription()
        {
            SimpleJobSchedule sjs = SimpleJobSchedule.FromJobScheduleData(this);
            return sjs.ToString();
        }
        #endregion

        #region static constants
        /// <summary>
        /// Maximum date supported by SSMS.
        /// This is the same as the culture max date because SQl Agent range is larger than all cultures' ranges.
        /// </summary>
        public static DateTime MaxAgentDateValue
        {
            get
            {
                return DateTime.Now; // Utils.GetMaxCultureDateTime().Date;
            }
        }
        /// <summary>
        /// Maximum timespan for a SqlAgent job.
        /// </summary>
        public static TimeSpan MaxAgentTimeValue
        {
            get
            {
                return new TimeSpan(0, 23, 59, 59);
            }
        }
        #endregion
    }
}
