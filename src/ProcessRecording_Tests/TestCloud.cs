﻿using System;
using System.IO;
using Newtonsoft.Json;
using GM.ProcessRecording;
using GM.FileDataModel;
using GM.FileDataRepositories;
using GM.Configuration;
using Microsoft.Extensions.Options;

namespace GM.ProcessRecording_Tests

{
    class TestCloud
    {
        private string language = "en";
        private IOptions<AppSettings> config;

        public void TestAll()
        {
            config.Value.DatafilesPath = Environment.CurrentDirectory + @"\..\..\Datafiles";
            config.Value.TestfilesPath = Environment.CurrentDirectory + @"\..\..\testdata";
            config.Value.GoogleApplicationCredentials = Environment.CurrentDirectory + @"..\\..\\..\\..\\..\\..\\..\\..\\_SECRETS\\TranscribeAudio.json";

            TestMoveToCloudAndTranscribe(language);
            TestTranscriptionOfFileInCloud(language);
            TestTranscriptionOfLocalFile(language);
        }

        public void TestMoveToCloudAndTranscribe(string language)
        {
            string baseName = "USA_ME_LincolnCounty_BoothbayHarbor_Selectmen_EN_2017-02-15";
            string videoFile = config.Value.TestfilesPath + "\\" + baseName + ".mp4";
            string outputFolder = config.Value.TestfilesPath + "\\" + "TestMoveToCloudAndTranscribe";

            FileDataRepositories.GMFileAccess.DeleteAndCreateDirectory(outputFolder);

            string outputBasePath = outputFolder + "\\" + baseName;
            string shortFile = outputBasePath + ".mp4";
            string audioFile = outputBasePath + ".flac";
            string jsonFile = outputBasePath + ".json";


            // Extract short version
            SplitRecording splitRecording = new SplitRecording();
            splitRecording.ExtractPart(videoFile, shortFile, 60, 4 * 60);

            // Extract audio.
            ExtractAudio extract = new ExtractAudio();
            extract.Extract(shortFile, audioFile);

            // Transcribe
            TranscribeAudio ta = new TranscribeAudio(config);
            TranscribeResponse response = ta.MoveToCloudAndTranscribe(audioFile, baseName + ".flac", language);

            string stringValue = JsonConvert.SerializeObject(response, Formatting.Indented);
            File.WriteAllText(outputBasePath + "-rsp.json", stringValue);

            // Modify Transcript json format
            ModifyTranscriptJson mt = new ModifyTranscriptJson();
            FixasrView fixasr = mt.Modify(response);

            // Create JSON file
            stringValue = JsonConvert.SerializeObject(fixasr, Formatting.Indented);
            File.WriteAllText(jsonFile, stringValue);
        }

        public void TestTranscriptionOfFileInCloud(string language)
        {
            TranscribeAudio ta = new TranscribeAudio(config);

            // Test transcription of a file already in the cloud storage bucket
            TranscribeResponse transcript = ta.TranscribeInCloud("USA_ME_LincolnCounty_BoothbayHarbor_Selectmen_EN_2017-01-09_00-01-40.flac", "en");
            //TranscribeResponse transcript = ta.TranscribeInCloud("Step 0 original#00-06-40.flac", "en");

            string stringValue = JsonConvert.SerializeObject(transcript, Formatting.Indented);
        }

        public void TestTranscriptionOfLocalFile(string language)
        {
            TranscribeAudio ta = new TranscribeAudio(config);

            // Test transcription on a local file. We will use sychronous calls to the Google Speech API. These allow a max of 1 minute per request.
            string folder = config.Value.TestfilesPath + @"..\testdata\BBH Selectmen\USA_ME_LincolnCounty_BoothbayHarbor_Selectmen\2017-01-09\step 2 extract\";
            TranscribeResponse transcript = ta.TranscribeFile(folder + "USA_ME_LincolnCounty_BoothbayHarbor_Selectmen_EN_2017-01-09#00-01-40.flac", language);

            string stringValue = JsonConvert.SerializeObject(transcript, Formatting.Indented);
        }
    }
}
