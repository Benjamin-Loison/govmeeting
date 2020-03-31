﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GM.ProcessRecording;
using GM.ProcessTranscript;
using GM.ViewModels;
using Microsoft.Extensions.Options;
using GM.Configuration;
using GM.FileDataRepositories;
using GM.DatabaseRepositories;
using GM.DatabaseModel;

namespace GM.Workflow
{
    public class ProcessTranscripts
    {

        /*   ProcessIncomingFiles watches the "RECEIVED" folder for files to be processed.
         *   Currently the file types can be either PDF or MP4.
         *   The names of the files must be in the format: <country>_<state>_<county>_<town-or-city>_<gov-entity>_<language>_<date>.<extension>
         *   For example:  USA_TX_TravisCounty_Austin_CityCouncil_en_2017-12-14.pdf
         * It creates a work folder in the Datafiles folder based on the name of the file.
         *    For example: USA_TX_TravisCounty_Austin_CityCouncil_en/2017-12-14
         * For new MP4 files, it calls: ProcessRecording
         * For new PDF files, it calls: ProcessTranscript
        */

        AppSettings _config;
        MeetingFolder meetingFolder;
        TranscriptProcess transcriptProcess;
        IMeetingRepository meetingRepository;
        IGovBodyRepository govBodyRepository;

        public ProcessTranscripts(
            IOptions<AppSettings> config,
            TranscriptProcess _transcriptProcess,
            MeetingFolder _meetingFolder,
            IMeetingRepository _meetingRepository,
            IGovBodyRepository _govBodyRepository
           )
        {
            _config = config.Value;
            meetingFolder = _meetingFolder;
            transcriptProcess = _transcriptProcess;
            meetingRepository = _meetingRepository;
            govBodyRepository = _govBodyRepository;
        }

        // Watch the incoming folder and process new files as they arrive.
        public void Run()
        {

            List<Meeting> meetings = meetingRepository.FindAll(SourceType.Transcript, WorkStatus.Received, true);

            foreach (Meeting meeting in meetings)
            {
                    doWork(meeting);
            }

        }

        public void doWork(Meeting meeting)
        {

            GovernmentBody g = govBodyRepository.Get(meeting.GovernmentBodyId);
            string language = g.Languages[0].Name;
            meetingFolder.SetFields(g.Country, g.State, g.County, g.Municipality, meeting.Date, g.Name, language);

            string workFolderPath = _config.DatafilesPath + "\\PROCESSING\\" + meetingFolder.path;

            // FOR DEVELOPMENT: WE DELETE PRIOR MEETING FOLDER IF IT EXISTS.
            //if (_config.IsDevelopment)
            //{
            //    FileDataRepositories.GMFileAccess.DeleteDirectoryAndContents(meetingFolder);

            //}

            if (!FileDataRepositories.GMFileAccess.CreateDirectory(workFolderPath))
            {
                // We were not able to create a folder for processing this video.
                // Probably because the folder already exists.
                Console.WriteLine("ProcessTranscriptsFiles.cs - ERROR: could not create work folder");
                return;
            }

            string sourceFilePath = _config.DatafilesPath + "\\RECEIVED\\" + meeting.SourceFilename;
            transcriptProcess.Process(sourceFilePath, workFolderPath, language);
        }

        //private void MoveFileToProcessedFolder(string filename)
        //{
        //    string processedPath = _config.DatafilesPath + @"\COMPLETED";
        //    string newFile = processedPath + "\\" + Path.GetFileName(filename);
        //    File.Move(filename, newFile);
        //}
    }
}
