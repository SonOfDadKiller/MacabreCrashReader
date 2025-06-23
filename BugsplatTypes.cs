using System;
using System.Collections.Generic;

namespace MacabreCrashReader
{
    public class PageData
    {
        public bool defectTracker { get; set; }
        public string defectTrackerType { get; set; }
    }

    public class BugsplatCrashes
    {
        public string database { get; set; }
        public PageData pageData { get; set; }
        public List<BugsplatCrashesRow> rows { get; set; }
    }

    public class BugsplatCrashesRow
    {
        public string id { get; set; }
        public string status { get; set; }
        public string stackId { get; set; }
        public string stackKey { get; set; }
        public string stackKeyId { get; set; }
        public string appName { get; set; }
        public string appVersion { get; set; }
        public string appDescription { get; set; }
        public string userDescription { get; set; }
        public string user { get; set; }
        public string email { get; set; }
        public string IpAddress { get; set; }
        public DateTime crashTime { get; set; }
        public object defectId { get; set; }
        public string defectUrl { get; set; }
        public string defectLabel { get; set; }
        public object skDefectId { get; set; }
        public string skDefectUrl { get; set; }
        public string skDefectLabel { get; set; }
        public string Comments { get; set; }
        public object skComments { get; set; }
        public string crashTypeId { get; set; }
        public string exceptionCode { get; set; }
        public string exceptionMessage { get; set; }
        public string attributes { get; set; }
        public object lineNumber { get; set; }
        public object groupByCount { get; set; }
    }

    public class BugsplatCrash
    {
        public int id { get; set; }
        public int status { get; set; }
        public int stackKeyId { get; set; }
        public string stackKey { get; set; }
        public string dumpfile { get; set; }
        public string appName { get; set; }
        public string appVersion { get; set; }
        public object appKey { get; set; }
        public string platform { get; set; }
        public DateTime crashTime { get; set; }
        public object user { get; set; }
        public object email { get; set; }
        public object description { get; set; }
        public string ipAddress { get; set; }
        public string processor { get; set; }
        public object comments { get; set; }
        public string exceptionCode { get; set; }
        public object defectId { get; set; }
        public object stackKeyDefectId { get; set; }
        public string exceptionMessage { get; set; }
        public object stackKeyComment { get; set; }
        public string attributes { get; set; }
        public string defectTrackerType { get; set; }
        public string defectUrl { get; set; }
        public string defectLabel { get; set; }
        public string stackKeyDefectUrl { get; set; }
        public string stackKeyDefectLabel { get; set; }
        public List<object> events { get; set; }
        public int dumpfileSize { get; set; }
        public int processed { get; set; }
        public object nextCrashId { get; set; }
        public int previousCrashId { get; set; }
        public bool missingSymbols { get; set; }
        public object debuggerOutput { get; set; }
        public BugsplatCrashThread thread { get; set; }
    }

    public class BugsplatCrashStackFrame
    {
        public int stackFrameLevel { get; set; }
        public int lineNumber { get; set; }
        public string functionName { get; set; }
        public string fileName { get; set; }
    }

    public class BugsplatCrashThread
    {
        public string threadId { get; set; }
        public int stackId { get; set; }
        public int subKeyDepth { get; set; }
        public List<BugsplatCrashStackFrame> stackFrames { get; set; }
    }
}
