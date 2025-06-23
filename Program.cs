using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MacabreCrashReader
{
    class CVarIncidence
    {
        public string Name;
        public string Value;
        public int Count;
        public float FractionOfCrashes;

        public CVarIncidence()
        {
            Name = "";
            Value = "";
            Count = 0;
            FractionOfCrashes = 0;
        }
    }
    
    class VideoAdapter
    {
        public string Vendor;
        public string Model;
        public string Driver;
        public int RamMegabytes;
        public DateTime DriverDate;

        public VideoAdapter()
        {
            Vendor = "";
            Model = "";
            RamMegabytes = 0;
            Driver = "";
            DriverDate = DateTime.MinValue;
        }
    }
    
    enum SessionType { Success, Crash, Incomplete }
    struct Session()
    {
        public string ID = "";
        public string User = "";
        public string LogFileID = "";
        public SessionType Type = SessionType.Incomplete;
        public DateTime StartDate = DateTime.MinValue;
        public DateTime EndDate = DateTime.MinValue;
        public string Build = "";
        public DateTime BuildDate = DateTime.MinValue;
        public TimeSpan RunningTime = TimeSpan.Zero;
        public string OS = "";
        public VideoAdapter VideoAdapter = new();
        public string CrashMessage = "";
        public Dictionary<string, string> CVars = new();
        public List<PerformanceReport> PerformanceReports = [];
    }

    class BuildStabilityReport()
    {
        public string Build = "";
        public DateTime BuildDate = DateTime.Now;
        public int NumSessions = 0;
        public int NumSuccesses = 0;
        public int NumCrashes = 0;
        public float CrashPercentage = 0f;
        public TimeSpan MinCrashTime = TimeSpan.MaxValue;
        public TimeSpan MaxCrashTime = TimeSpan.MinValue;
        public TimeSpan AvgCrashTime = TimeSpan.Zero;
    }

    class BuildToGPUCrashReport()
    {
        public string Build = "";
        public DateTime BuildDate = DateTime.MinValue;
        public string GPUModel = "";
        public int CrashCount = 0;
        public int SuccessCount = 0;
        public float CrashChance = 0f;
        public TimeSpan MinCrashTime = TimeSpan.MaxValue;
        public TimeSpan MaxCrashTime = TimeSpan.MinValue;
        public TimeSpan AvgCrashTime = TimeSpan.Zero;
    }

    class GPUToCVarCrashReport()
    {
        public string GPUModel = "";
        public string Key = "";
        public string Value = "";
        public int CrashCount = 0;
        public int SuccessCount = 0;
        public float CrashChance = 0f;
    }
    
    enum GraphicsSettingLevel { Low, Medium, High, Epic, Cinematic, Unknown }

    class GraphicsSettings()
    {
        public GraphicsSettingLevel ResolutionQuality = GraphicsSettingLevel.Unknown;
        public GraphicsSettingLevel AntiAliasingQuality = GraphicsSettingLevel.Unknown;
        public GraphicsSettingLevel ViewDistanceQuality =  GraphicsSettingLevel.Unknown;
        public GraphicsSettingLevel ShadowQuality = GraphicsSettingLevel.Unknown;
        public GraphicsSettingLevel GIQuality = GraphicsSettingLevel.Unknown;
        public GraphicsSettingLevel ReflectionQuality = GraphicsSettingLevel.Unknown;
        public GraphicsSettingLevel PostProcessingQuality = GraphicsSettingLevel.Unknown;
        public GraphicsSettingLevel TextureQuality = GraphicsSettingLevel.Unknown;
        public GraphicsSettingLevel EffectsQuality = GraphicsSettingLevel.Unknown;
        public GraphicsSettingLevel FoliageQuality = GraphicsSettingLevel.Unknown;
        public GraphicsSettingLevel ShadingQuality = GraphicsSettingLevel.Unknown;
    }
    
    class PerformanceReport()
    {
        public GraphicsSettings Settings = new GraphicsSettings();
        public float MinTimeMS = 0f;
        public float MaxTimeMS = 0f;
        public float AvgFPS = 0f;
    }

    class GPUPerformanceReport()
    {
        public string GPUModel = "";
        public GraphicsSettings Settings = new GraphicsSettings();
        public float AvgFPS = 0f;
    }

    internal class Program
    {
        private static bool DownloadNewArchives = true;
        private static bool CheckAllDownloadPages = false;
        private static bool AddDoubleUpsToIgnoreList = true;

        private static DateTime StartDate = new DateTime(2025, 6, 9, 17, 0, 0);
        private static DateTime EndDate = DateTime.Now;

        private static string AuthToken =
            "Bearer eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiJ9.eyJhdWQiOiI3NDA4ZTIxNmI5YmNmZGYwZDE2NzRkNWRkNTVmZjI2YSIsImp0aSI6IjJjNGEyZWI4NzE0MTBkZWJhMmM2NWVmYmQ2NTk2ZjRhNjMyZDUxYjhmMzdlMWNlODM1NDRhYjVlM2UxYzZmZTk1YTIxM2YwMTUzMDk2YTE5IiwiaWF0IjoxNzUwNjQxNDgzLjg0OTkxMywibmJmIjoxNzUwNjQxNDgzLjg0OTkxNywiZXhwIjoxNzUwNzI3ODgzLjgzODc0Miwic3ViIjoiIiwic2NvcGVzIjpbInJlc3RyaWN0ZWQiXX0.bRUd0etDIESWyVigR2H8rs5XxIs7etTANeHTP-0moCG91pZX2gSL75-MCkFglzu09jOZZ6JT_00RFCNVdw1YhM1rIirduoLsnKdDBHl6hbc0OMewyh0Q8xUF2vPIIXxtwdOCaO3KccifT0Dn_cJRZovZTOW_CKTqJ4tF17-aSWcEh5kf-0ABWYlEIShnvTQ_2DVj4PpKeCzKDEmYLNm6OhRwlkcSTizWIEANJDYSOCVPYhTY5R-4JaKnlNIA6iBxUTNK7B3qUwDqD2cMK_0YetvCRTKepWwSIo18A6YDwEkcwpVD7q7zKj6HTKKjBkTwFxBIWwvJfEArnb3oEyLh8w";
        private static string Cookie = "PHPSESSID=de3a02abd8875671d90c354ff39c284f";
        
        static string GetBetween(string Input, string Left, string Right)
        {
            string FirstSplit = Input.Split([Left], StringSplitOptions.RemoveEmptyEntries)[1];
            return FirstSplit.Split([Right], StringSplitOptions.RemoveEmptyEntries)[0].Trim();
        }

        static string GetAfter(string Input, string SplitOn)
        {
            return Input.Split([SplitOn], StringSplitOptions.RemoveEmptyEntries)[1];
        }

        static DateTime GetLineDate(string Line)
        {
            string FirstSplit = Line.Split(']')[0];
            string DateTimeString = FirstSplit.TrimStart('[').Split(':')[0];
            return DateTime.ParseExact(DateTimeString, "yyyy.MM.dd-HH.mm.ss", CultureInfo.InvariantCulture);
        }
        
        public static async Task Main(string[] args)
        {
            string ExecutionPath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location); 
            Console.WriteLine(ExecutionPath);
            
            // Check reports folder for existing reports
            string ArchivePath = "D:/Macabre Crashes/Archives";
            if (!Directory.Exists(ArchivePath))
            {
                Directory.CreateDirectory(ArchivePath);
            }
            
            // Check ignore file for ignored reports
            string IgnoreFile = "D:/Macabre Crashes/ignore.txt";
            List<string> ReportsToIgnore = [];
            if (File.Exists(IgnoreFile))
            {
                foreach (string Line in File.ReadLines(IgnoreFile))
                {
                    ReportsToIgnore.Add(Line);
                }
            }
            else
            {
                File.Create(IgnoreFile).Dispose();
            }
            
            List<string> ArchiveFiles = Directory.GetFiles(ArchivePath).ToList();
            List<string> ExistingReportFileIDs = new List<string>();

            foreach (string ArchiveFile in ArchiveFiles)
            {
                ExistingReportFileIDs.Add(Path.GetFileNameWithoutExtension(ArchiveFile));
            }

            if (DownloadNewArchives)
            {
                // Download crashes from bugsplat
                HttpClient Client = new HttpClient();
                List<string> NewBugsplatCrashIDs = new List<string>();

                // Iterate over pages until we reach crash report before the period beginning
                Console.WriteLine($"Requesting bugsplat reports from {StartDate} until {EndDate}...");
                int Page = 0;
                bool Complete = false;
                while (!Complete)
                {
                    Console.Write($"\rRequesting page {Page}...");
                    HttpRequestMessage Request = new HttpRequestMessage(HttpMethod.Get,
                        $"https://app.bugsplat.com/api/crashes?pagenum={Page}&pagesize=50");
                    Request.Headers.Add("Authorization", AuthToken);
                    Request.Headers.Add("Cookie", Cookie);
                    HttpResponseMessage Response = await Client.SendAsync(Request);

                    //Console.WriteLine(await Response.Content.ReadAsStringAsync());

                    if (Response.IsSuccessStatusCode)
                    {
                        //Console.WriteLine("Page received successfully");
                        string Content = await Response.Content.ReadAsStringAsync();
                        //Console.WriteLine(Content);
                        
                        BugsplatCrashes Crashes = JsonConvert.DeserializeObject<BugsplatCrashes>(Content);

                        foreach (BugsplatCrashesRow Row in Crashes.rows)
                        {
                            if (!ExistingReportFileIDs.Contains(Row.id) && !ReportsToIgnore.Contains(Row.id))
                            {
                                NewBugsplatCrashIDs.Add(Row.id);

                                if (Row.crashTime < StartDate || Row.crashTime > EndDate)
                                {
                                    Complete = true;
                                }
                            }
                            else if (!CheckAllDownloadPages)
                            {
                                Complete = true;
                            }
                        }

                        Page++;
                    }
                    else
                    {
                        Console.WriteLine(
                            $"Page request failed '{Response.StatusCode} - {Response.ReasonPhrase}', trying again");
                    }
                }

                Console.WriteLine($"\rFound {NewBugsplatCrashIDs.Count} new crash reports in {Page + 1} pages");

                // Reverse list so we download logs in chronological order
                NewBugsplatCrashIDs.Reverse();

                SemaphoreSlim Semaphore = new SemaphoreSlim(4);
                List<Task> DownloadTasks = new List<Task>();

                for (int i = 0; i < NewBugsplatCrashIDs.Count; i++)
                {
                    string CrashID = NewBugsplatCrashIDs[i];
                    
                    DownloadTasks.Add(Task.Run(async () =>
                    {
                        await Semaphore.WaitAsync();
                        try
                        {
                            retry:
                            Console.Write($"\rDownloading crash {i}/{NewBugsplatCrashIDs.Count}: {CrashID}...");
                            HttpRequestMessage Request = new HttpRequestMessage(HttpMethod.Get,
                                $"https://app.bugsplat.com/api/crash/details?database=MacabreBugsDefault&id={CrashID}");
                            Request.Headers.Add("Authorization", AuthToken);
                            Request.Headers.Add("Cookie", Cookie);

                            HttpResponseMessage Response;
                            try
                            {
                                Response = await Client.SendAsync(Request);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine($"Request failed: {e.Message}, retrying...");
                                await Task.Delay(100);
                                goto retry;
                            }

                            if (Response.IsSuccessStatusCode)
                            {
                                // TODO: Determine from bugsplat if the report is a crash or not, based on other info, that way we can still know in the case of an incomplete log file
                                string Content = await Response.Content.ReadAsStringAsync();
                                BugsplatCrash Crash = JsonConvert.DeserializeObject<BugsplatCrash>(Content);

                                if (Crash.dumpfile == null)
                                {
                                    Console.WriteLine($"Crash report {CrashID} has no dump file");
                                    return;
                                }

                                try
                                {
                                    using var DownloadStream = await Client.GetStreamAsync(Crash.dumpfile);
                                    using var FileStream = new FileStream(Path.Combine(ArchivePath, CrashID + ".zip"), FileMode.Create, FileAccess.Write, FileShare.None);
                                    await DownloadStream.CopyToAsync(FileStream);
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e);
                                    throw;
                                }
                            }
                            else
                            {
                                Console.Write($"Crash request failed '{Response.StatusCode} - {Response.ReasonPhrase}', trying again");
                                await Task.Delay(1000);
                                goto retry;
                            }
                        }
                        finally
                        {
                            Semaphore.Release();
                        }
                    }));
                }

                await Task.WhenAll(DownloadTasks);
            }

            string LogPath = "D:/Macabre Crashes/Logs";
            if (!Directory.Exists(LogPath))
            {
                Directory.CreateDirectory(LogPath);
            }

            // Load all archive files again, and filter for ones that we don't have log files for
            List<string> ExistingLogFiles = Directory.GetFiles(LogPath).ToList();
            List<string> ExistingLogIDs = new List<string>();
            foreach (string LogFile in ExistingLogFiles)
            {
                ExistingLogIDs.Add(Path.GetFileNameWithoutExtension(LogFile));
            }

            ArchiveFiles = Directory.GetFiles(ArchivePath).ToList();
            
            // Ignore archives that have already been extracted, and ones that are in the ignore list
            ArchiveFiles.RemoveAll(x => ExistingLogIDs.Contains(Path.GetFileNameWithoutExtension(x)));
            ArchiveFiles.RemoveAll(x => ReportsToIgnore.Contains(Path.GetFileNameWithoutExtension(x)));
            
            // Unzip text file contents found in crash zip files, if they do not yet exist in the database
            for (int i = 0; i < ArchiveFiles.Count; i++)
            {
                string ZipFilePath = ArchiveFiles[i];
                string CrashID = Path.GetFileNameWithoutExtension(ZipFilePath);

                string LogFilePath = Path.Combine(LogPath, CrashID + ".txt"); 
                if (!File.Exists(LogFilePath))
                {
                    Console.Write($"\rExtracting log from archive {i}/{ArchiveFiles.Count} with id {CrashID}...");

                    ZipArchive Archive = ZipFile.OpenRead(ZipFilePath);
                    foreach (ZipArchiveEntry Entry in Archive.Entries)
                    {
                        if (Entry.FullName == "Macabre.log" || Entry.FullName == "bsCrashReport.xml")
                        {
                            Entry.ExtractToFile(LogFilePath);
                        }
                    }
                }
            }

            Console.WriteLine("\nParsing log files...");
            ConcurrentBag<Session> Sessions = [];
            ConcurrentBag<string> FoundSessionIDs = [];
            List<string> CrashLogPaths = Directory.GetFiles(LogPath).ToList();
            object SessionLock = new object();
            int FilesProcessed = 0;

            // Filter out existing session files
            string SessionPath = "D:/Macabre Crashes/Sessions";
            if (!Directory.Exists(SessionPath))
            {
                Directory.CreateDirectory(SessionPath);
            }
            
            List<string> ExistingSessionFiles = Directory.GetFiles(SessionPath).ToList();
            List<string> ExistingSessionIDs = new List<string>();
            foreach (string SessionFile in ExistingSessionFiles)
            {
                ExistingSessionIDs.Add(Path.GetFileNameWithoutExtension(SessionFile));
            }
           
            CrashLogPaths.RemoveAll(x => ExistingSessionIDs.Contains(Path.GetFileNameWithoutExtension(x)));
            
            // Parse text files
            try
            {
                Parallel.For(0, CrashLogPaths.Count, i =>
                {
                    string CrashLogFile = CrashLogPaths[i];
                    
                    Session Session = new Session();
                    Session.LogFileID = Path.GetFileNameWithoutExtension(CrashLogFile);
                    List<VideoAdapter> VideoAdapters = new List<VideoAdapter>();
                    string LastDateLine = "";

                    PerformanceReport CurrentPerfReport = null;

                    StreamReader Reader = new StreamReader(CrashLogFile);
                    string Line = Reader.ReadLine();
                    while (Line != null)
                    {
                        // Operating System
                        if (Line.StartsWith("LogInit: OS"))
                        {
                            string[] SplitByColon = Line.Split(':');
                            string[] OSWords = SplitByColon[2].Split(' ');
                            Session.OS = OSWords[1] + " " + OSWords[2];

                            Session.VideoAdapter.Model = SplitByColon[SplitByColon.Length - 1].TrimStart();
                            Session.VideoAdapter.Vendor = Session.VideoAdapter.Model.Split(' ')[0];
                            Line = Reader.ReadLine();
                            continue;
                        }

                        // Project version
                        if (Line.Contains("Set ProjectVersion"))
                        {
                            string[] VersionStringSplit = GetBetween(Line, "to ", ". Version").Split('_');
                            string BuildCommitHash = VersionStringSplit[0];
                            string BuildDateTime = VersionStringSplit[1] + "_" + VersionStringSplit[2];
                            //Console.WriteLine(BuildDateTime);
                            Session.Build = BuildCommitHash;
                            Session.BuildDate = DateTime.ParseExact(BuildDateTime, "dd-MM-yyyy_HH-mm-ss", null);
                            Line = Reader.ReadLine();
                            continue;
                        }

                        // Steam ID
                        // TODO: Still try to get steam user name so we can print it, but use ID from game instance to distinguish users
                        // TODO: In fact, we can build up a table to match usernames to IDs, so we can determine the name from an ID for cases where only the ID is known
                        if (Session.User == "" && Line.Contains("Creating Talker for player"))
                        {
                            string[] Split = Line.Split(' ');
                            Session.User = Split[Split.Length - 1];
                            //Console.WriteLine("Found Steam ID " + Session.User);
                            Line = Reader.ReadLine();
                            continue;
                        }

                        // Determine start time from first log with timestamp
                        if (Session.StartDate == DateTime.MinValue && Line.StartsWith("[2025."))
                        {
                            Session.StartDate = GetLineDate(Line);
                            Line = Reader.ReadLine();
                            continue;
                        }

                        // Video adapter definition
                        if (Line.StartsWith("LogD3D12RHI: Found D3D12 adapter"))
                        {
                            VideoAdapters.Add(new VideoAdapter());
                            Line = Reader.ReadLine();
                            continue;
                        }

                        // Video adapter memory
                        if (Line.StartsWith("LogD3D12RHI:   Adapter has"))
                        {
                            string Memory = GetBetween(Line, "and", "of shared system memory");
                            Memory = Memory.TrimEnd('M', 'B');
                            VideoAdapters[VideoAdapters.Count - 1].RamMegabytes = int.Parse(Memory);
                            Line = Reader.ReadLine();
                            continue;
                        }

                        // Video adapter driver version
                        if (Line.StartsWith("LogD3D12RHI:   Driver Version"))
                        {
                            string[] VersionLineSplit = Line.Split(':');
                            string VersionString = "";
                            for (int j = 2; j < VersionLineSplit.Length; j++)
                            {
                                VersionString += VersionLineSplit[j];
                            }

                            VideoAdapters[VideoAdapters.Count - 1].Driver = VersionString.Replace(',', ' ');

                            Line = Reader.ReadLine();
                            continue;
                        }

                        // Video adapter driver
                        if (Line.StartsWith("LogD3D12RHI:      Driver Date"))
                        {
                            if (Line.Contains("Unknown"))
                            {
                                VideoAdapters[VideoAdapters.Count - 1].DriverDate = DateTime.MinValue;
                            }
                            else
                            {
                                string[] DateLineSplit = Line.Split(':');
                                string DateString = DateLineSplit[2].Trim();
                                string[] DateStringSplit = DateString.Split('-');
                                DateTime Date = new DateTime(int.Parse(DateStringSplit[2]), int.Parse(DateStringSplit[0]),
                                    int.Parse(DateStringSplit[1]));
                                VideoAdapters[VideoAdapters.Count - 1].DriverDate = Date;
                            }

                            Line = Reader.ReadLine();
                            continue;
                        }

                        // Video adapter chosen
                        if (Line.StartsWith("LogD3D12RHI: Chosen D3D12 Adapter Id"))
                        {
                            string[] LineSplit = Line.Split('=');
                            int AdapterIndex = int.Parse(LineSplit[1].Trim());
                            Session.VideoAdapter = VideoAdapters[AdapterIndex];
                            Line = Reader.ReadLine();
                            continue;
                        }

                        // Read CVars
                        if (Line.Contains("Set CVar"))
                        {
                            string[] KeyVal = GetBetween(Line, "[[", "]]").Split(':');

                            if (KeyVal[1].Contains(';'))
                            {
                                KeyVal[1] = KeyVal[1].Split(';')[0].Trim();
                            }

                            Session.CVars[KeyVal[0]] = KeyVal[1];
                            Line = Reader.ReadLine();
                            continue;
                        }
                        
                        // Performance Report
                        if (Line.Contains("Framerate Report"))
                        {
                            CurrentPerfReport = new PerformanceReport();
                            // float Duration = float.Parse(Reader.ReadLine().Split('=')[1].TrimEnd('s'));
                            // int Frames = int.Parse(Reader.ReadLine().Split('=')[1]);
                            // CurrentPerfReport.MinTimeMS = float.Parse(Reader.ReadLine().Split('=')[1].TrimEnd(['m', 's']));
                            // CurrentPerfReport.MaxTimeMS = float.Parse(Reader.ReadLine().Split('=')[1].TrimEnd(['m', 's']));
                            // CurrentPerfReport.AvgFPS = float.Parse(Reader.ReadLine().Split('=')[1]);
                            // Session.PerformanceReports.Add(CurrentPerfReport);
                            // CurrentPerfReport = null;
                            Line = Reader.ReadLine();
                            continue;
                        }

                        if (CurrentPerfReport != null)
                        {
                            if (Line.Contains("Warning: Duration="))
                            {
                                float Duration = float.Parse(Line.Split('=')[1].TrimEnd('s'));
                                Line = Reader.ReadLine();
                                continue;
                            }
                            
                            if (Line.Contains("Warning: Frames="))
                            {
                                int Frames = int.Parse(Line.Split('=')[1]);
                                Line = Reader.ReadLine();
                                continue;
                            }
                            
                            if (Line.Contains("Warning: Min="))
                            {
                                CurrentPerfReport.MinTimeMS = float.Parse(Line.Split('=')[1].TrimEnd(['m', 's']));
                                Line = Reader.ReadLine();
                                continue;
                            }

                            if (Line.Contains("Warning: Max="))
                            {
                                CurrentPerfReport.MaxTimeMS = float.Parse(Line.Split('=')[1].TrimEnd(['m', 's']));
                                Line = Reader.ReadLine();
                                continue;
                            }

                            if (Line.Contains("Warning: AvgFPS="))
                            {
                                CurrentPerfReport.AvgFPS = float.Parse(Line.Split('=')[1]);
                                Session.PerformanceReports.Add(CurrentPerfReport);
                                CurrentPerfReport = null;
                                Line = Reader.ReadLine();
                                continue;
                            }
                        }

                        // Closing normally
                        if (Line.Contains("Alt-F4 pressed!") || Line.Contains("Closing by request"))
                        {
                            Session.Type = SessionType.Success;
                            Line = Reader.ReadLine();
                            continue;
                        }
                    
                        // Closing due to unknown reason (success?)
                        if (Session.Type != SessionType.Crash && Line.Contains("Engine exit requested"))
                        {
                            Session.Type = SessionType.Success;
                            Line = Reader.ReadLine();
                            continue;
                        }

                        // Closing due to GPU Crash
                        if (Line.Contains("LogD3D12RHI: Error: GPU crash detected:"))
                        {
                            Session.Type = SessionType.Crash;
                            string[] ErrorLineSplit = Reader.ReadLine().Split(' ');
                            Session.CrashMessage = ErrorLineSplit[ErrorLineSplit.Length - 1];
                            Line = Reader.ReadLine();
                            continue;
                        }
                    
                        // Closing due to other crashes
                        if (Line.Contains("LogWindows: Error: appError called: Fatal error:"))
                        {
                            //Session.EndDate = GetLineDate(Line);
                            Session.Type = SessionType.Crash;
                            Session.CrashMessage = GetAfter(Line, "Fatal error:");
                            Session.CrashMessage += Reader.ReadLine();
                            Line = Reader.ReadLine();
                            continue;
                        }

                        if (Line.Contains("=== Handled ensure: ==="))
                        {
                            //Session.EndDate = GetLineDate(Line);
                            Session.Type = SessionType.Crash;
                            Reader.ReadLine();
                            Session.CrashMessage = Reader.ReadLine();
                            Session.CrashMessage += " - " + Reader.ReadLine();
                            Line = Reader.ReadLine();
                            continue;
                        }
                    
                        // Get end time
                        if (Line.StartsWith("[20") && Line.Contains("]["))
                        {
                            LastDateLine = Line;
                        }

                        Line = Reader.ReadLine();
                    }

                    if (LastDateLine != "")
                    {
                        Session.EndDate = GetLineDate(LastDateLine);
                    }

                    if (Session.StartDate != DateTime.MinValue && Session.EndDate != DateTime.MinValue)
                    {
                        Session.RunningTime = Session.EndDate - Session.StartDate;
                    }

                    if (Session.User == "")
                    {
                        Console.WriteLine("\rCould not find user for file " + CrashLogFile);
                    }

                    // Determine session ID from steam user and start date if both were found, discard if session is a double-up
                    if (Session.User != "" && Session.StartDate != DateTime.MinValue)
                    {
                        Session.ID = Session.User + "_" + Session.StartDate.ToString();

                        lock (SessionLock)
                        {
                            if (FoundSessionIDs.Contains(Session.ID))
                            {
                                Console.WriteLine($"\rFound a double up for session {Session.ID}");
                                using (FileStream Stream = new FileStream(IgnoreFile, FileMode.Append))
                                using (StreamWriter Writer = new StreamWriter(Stream))
                                {
                                    Writer.WriteLine(Session.LogFileID);
                                }
                            }
                            else
                            {
                                FoundSessionIDs.Add(Session.ID);
                                Sessions.Add(Session);
                            }
                        }
                    }

                    int Progress = Interlocked.Increment(ref FilesProcessed);
                    Console.Write($"\rParsing log files... {Progress}/{CrashLogPaths.Count}");

                    Reader.Close();
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
            Console.WriteLine("\nSerializing sessions to disk...");
            
            // Serialize sessions to disk, then load all sessions
            foreach (Session Session in Sessions)
            {
                string SessionJSON = JsonConvert.SerializeObject(Session);
                StreamWriter SessionOutput = new StreamWriter(Path.Combine(SessionPath, Session.LogFileID + ".txt"));
                SessionOutput.WriteLine(SessionJSON);
                SessionOutput.Flush();
                SessionOutput.Close();
            }

            Sessions = [];
            List<string> SessionFiles = Directory.GetFiles(SessionPath).ToList();
            foreach (string SessionFile in SessionFiles)
            {
                StreamReader Reader = new StreamReader(SessionFile);
                string Content = Reader.ReadLine();
                Sessions.Add(JsonConvert.DeserializeObject<Session>(Content));
                Reader.Close();
            }

            int NumSuccessfulSessions = 0;
            int NumCrashSessions = 0;
            int NumIncompleteSessions = 0;
            
            Console.WriteLine("Generating session list...");

            // Output to CSV file
            StreamWriter CSVOutput = new StreamWriter("D:/Macabre Crashes/Sessions.csv");
            CSVOutput.WriteLine("Session ID, User, Log File ID, Type, Build, Build Date, Session Start, Session End, Running Time, Operating System, GPU Vendor, GPU Model, GPU RAM MB, GPU Driver Version, GPU Driver Date, Crash Message");
            
            foreach (Session Session in Sessions)
            {
                String InfoLine =
                    Session.ID
                    + "," + Session.User
                    + "," + Session.LogFileID
                    + "," + Session.Type
                    + "," + Session.Build
                    + "," + Session.BuildDate.ToShortDateString()
                    + "," + Session.StartDate.ToShortDateString()
                    + "," + Session.EndDate.ToShortDateString()
                    + "," + Session.RunningTime
                    + "," + Session.OS
                    + "," + Session.VideoAdapter.Vendor
                    + "," + Session.VideoAdapter.Model
                    + "," + Session.VideoAdapter.RamMegabytes
                    + "," + Session.VideoAdapter.Driver
                    + "," + Session.VideoAdapter.DriverDate.ToShortDateString()
                    + "," + Session.CrashMessage;

                if (Session.Type == SessionType.Success)
                {
                    NumSuccessfulSessions++;
                }
                else if (Session.Type == SessionType.Crash)
                {
                    NumCrashSessions++;
                }
                else if (Session.Type == SessionType.Incomplete)
                {
                    NumIncompleteSessions++;
                    //Console.WriteLine($"Found incomplete session: {Session.ID}, {Session.LogFileID}");
                }
                
                CSVOutput.WriteLine(InfoLine);
                //Console.WriteLine(InfoLine);
            }
            
            Console.WriteLine($"Successful Sessions: {NumSuccessfulSessions}");
            Console.WriteLine($"Crash Sessions: {NumCrashSessions}");
            Console.WriteLine($"Incomplete Sessions: {NumIncompleteSessions}");
            if (NumSuccessfulSessions != 0)
            {
                Console.WriteLine($"Known Crash Rate: {((float)NumCrashSessions / (NumSuccessfulSessions + NumCrashSessions)) * 100f}%");
            }
            
            CSVOutput.Flush();
            
            Console.WriteLine("Generating incedence report...");
            
            // Generate CVar incidence information
            Dictionary<string, CVarIncidence> Incidences = new Dictionary<string, CVarIncidence>();
            foreach (Session Info in Sessions)
            {
                foreach (var CVarPair in Info.CVars)
                {
                    string IncidenceKey = CVarPair.Key + CVarPair.Value;
                    if (!Incidences.ContainsKey(IncidenceKey))
                    {
                        CVarIncidence Incidence = new CVarIncidence();
                        Incidence.Name = CVarPair.Key;
                        Incidence.Value = CVarPair.Value;
                        Incidence.Count = 1;
                        Incidence.FractionOfCrashes = 1f / Sessions.Count;
                        Incidences[IncidenceKey] = Incidence;
                    }
                    else
                    {
                        CVarIncidence Incidence = Incidences[IncidenceKey];
                        Incidence.Count++;
                        Incidence.FractionOfCrashes = (float)Incidence.Count / Sessions.Count;
                    }
                }
            }
            
            CSVOutput = new StreamWriter("D:/Macabre Crashes/Incidence.csv");
            CSVOutput.WriteLine("Setting, Value, Count, Fraction");

            List<CVarIncidence> IncidenceValues = Incidences.Values.ToList();
            IncidenceValues = IncidenceValues.OrderBy((incidence => incidence.Count)).ToList();
            
            foreach (CVarIncidence Incidence in IncidenceValues)
            {
                string IncidenceInfo = 
                    Incidence.Name 
                    + "," + Incidence.Value
                    + "," + Incidence.Count
                    + "," + Incidence.FractionOfCrashes;
                
                CSVOutput.WriteLine(IncidenceInfo);
                //Console.WriteLine(IncidenceInfo);
            }
            
            CSVOutput.Flush();
            
            // Build to GPU Crash Report
            Console.WriteLine("Generating Build to GPU Crash Report...");
            
            SortedDictionary<string, BuildToGPUCrashReport> BuildToGpuCrashReports = [];
            foreach (Session Session in Sessions)
            {
                if (Session.Type == SessionType.Incomplete) { continue; }

                BuildToGPUCrashReport Report = null;
                string ReportKey = Session.BuildDate.Ticks + Session.VideoAdapter.Model;
                if (BuildToGpuCrashReports.ContainsKey(ReportKey))
                {
                    Report = BuildToGpuCrashReports[ReportKey];
                }
                else
                {
                    Report = new BuildToGPUCrashReport();
                    Report.Build = Session.Build;
                    Report.BuildDate = Session.BuildDate;
                    Report.GPUModel = Session.VideoAdapter.Model;
                    BuildToGpuCrashReports.Add(ReportKey, Report);
                }
                
                if (Session.Type == SessionType.Crash)
                {
                    Report.CrashCount++;
                    Report.AvgCrashTime += Session.RunningTime;
                    if (Session.RunningTime > Report.MaxCrashTime)
                    {
                        Report.MaxCrashTime = Session.RunningTime;
                    }
                    if (Session.RunningTime < Report.MinCrashTime)
                    {
                        Report.MinCrashTime = Session.RunningTime;
                    }
                }
                else
                {
                    Report.SuccessCount++;
                }
            }

            List<BuildToGPUCrashReport> ReportValues = BuildToGpuCrashReports.Values.ToList();
            ReportValues.Reverse();
            
            CSVOutput = new StreamWriter("D:/Macabre Crashes/BuildToGPUCrashReport.csv");
            CSVOutput.WriteLine("Build, Build Date, GPU Model, Crash Count, Success Count, Crash Percentage, Avg Crash Time, Min Crash Time, Max Crash Time");
            
            string BuildColumn = "";
            foreach (BuildToGPUCrashReport Report in ReportValues)
            {
                Report.CrashChance = (float)Report.CrashCount / Report.CrashCount + Report.SuccessCount;
                if (Report.CrashCount > 0)
                {
                    Report.AvgCrashTime = new TimeSpan(Report.AvgCrashTime.Ticks / Report.CrashCount);
                }

                string BuildToPrint = "";
                string BuildDateToPrint = "";
                if (BuildColumn != Report.Build)
                {
                    BuildToPrint = Report.Build;
                    BuildDateToPrint = Report.BuildDate.ToShortDateString();
                    BuildColumn = Report.Build;
                }
                
                CSVOutput.WriteLine($"{BuildToPrint}, {BuildDateToPrint}, {Report.GPUModel}, {Report.CrashCount}, {Report.SuccessCount}, {Report.CrashChance * 100f}%, {Report.AvgCrashTime}, {Report.MinCrashTime}, {Report.MaxCrashTime}");
            }
            
            CSVOutput.Flush();
            
            // Generating Build Stability Report
            Console.WriteLine("Generating Build Stability Report");

            Dictionary<string, BuildStabilityReport> BuildStabilityReports = new Dictionary<string, BuildStabilityReport>();
            foreach (Session Session in Sessions)
            {
                if (Session.Type == SessionType.Incomplete) { continue; }
                
                BuildStabilityReport Report;
                if (BuildStabilityReports.ContainsKey(Session.Build))
                {
                    Report = BuildStabilityReports[Session.Build];
                }
                else
                {
                    Report = new BuildStabilityReport();
                    Report.Build = Session.Build;
                    Report.BuildDate = Session.BuildDate;
                    BuildStabilityReports.Add(Session.Build, Report);
                }
                
                Report.NumSessions++;
                if (Session.Type == SessionType.Success)
                {
                    Report.NumSuccesses++;
                }
                else
                {
                    Report.NumCrashes++;
                    Report.AvgCrashTime += Session.RunningTime;
                    if (Session.RunningTime > Report.MaxCrashTime)
                    {
                        Report.MaxCrashTime = Session.RunningTime;
                    }
                    if (Session.RunningTime < Report.MinCrashTime)
                    {
                        Report.MinCrashTime = Session.RunningTime;
                    }
                }
            }

            List<BuildStabilityReport> BuildStabilityReportValues = BuildStabilityReports.Values.ToList();
            CSVOutput = new StreamWriter("D:/Macabre Crashes/BuildStabilityReport.csv");
            CSVOutput.WriteLine("Build, Build Date, Sessions, Successes, Crashes, Percentage, Min Crash Time, Max Crash Time, Avg Crash Time");
            
            BuildStabilityReportValues = BuildStabilityReportValues.OrderByDescending((Report) => Report.BuildDate).ToList();
            
            foreach (BuildStabilityReport Report in BuildStabilityReportValues)
            {
                string MinCrashTime;
                string MaxCrashTime;
                string AvgCrashTime;
                if (Report.NumCrashes > 0)
                {
                    Report.AvgCrashTime = new TimeSpan(Report.AvgCrashTime.Ticks / Report.NumCrashes);
                    Report.AvgCrashTime = TimeSpan.FromSeconds(Math.Round(Report.AvgCrashTime.TotalSeconds));
                    MinCrashTime = Report.MinCrashTime.ToString();
                    MaxCrashTime = Report.MaxCrashTime.ToString();
                    AvgCrashTime = Report.AvgCrashTime.ToString();
                }
                else
                {
                    MinCrashTime = "N/A";
                    MaxCrashTime = "N/A";
                    AvgCrashTime = "N/A";
                }
                Report.CrashPercentage = (float)Report.NumCrashes / Report.NumSessions;
                CSVOutput.WriteLine($"{Report.Build}, {Report.BuildDate}, {Report.NumSessions}, {Report.NumSuccesses}, {Report.NumCrashes}, {Report.CrashPercentage * 100}%, {MinCrashTime}, {MaxCrashTime}, {AvgCrashTime}");
            }
            
            CSVOutput.Flush();
            
            Console.WriteLine("we are done yay :)");
        }
    }
}