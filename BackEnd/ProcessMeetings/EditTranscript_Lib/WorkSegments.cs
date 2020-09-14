﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GM.ViewModels;
using GM.Utilities;

namespace GM.EditTranscript
{
    public class WorkSegments
    {
        AudioProcessing audioProcessing;

        public WorkSegments()
        {
            audioProcessing = new AudioProcessing();
        }

        /*   Split the video, audio and JSON data for this meeting into smaller
         *   segments. This allows multiple people to work on the fixes to 
         *   the text at the same time.
         */
        public void Split(string meetingFolder, string videofile, string fixasrFile,
            int segmentSize, int segmentOverlap)
        {
            string splitFolder = Path.Combine(meetingFolder,"FixText");

            // The processed recording will next go through the following workflow:
            //   1. Users will fix errors in the text generated by auto voice recognition.
            //   2. Users will add metadata tags to the transcript.
            // To facilitate this, we will split the video, audio and transcript files into smaller segments.
            // This has the advantages that:
            //   1. More than one volunteer can work on the recording at the same time.
            //   2. Less video or audio data needs to be downloaded to a user at one time.

            string stringValue = File.ReadAllText(fixasrFile);
            FixasrView fixasr = JsonConvert.DeserializeObject<FixasrView>(stringValue);

            // Split the recording into parts and put them each in subfolders of subfolder "parts".
            SplitRecording splitRecording = new SplitRecording();
            int parts = splitRecording.Split(videofile, splitFolder, segmentSize, segmentOverlap);

            // Also extract the audio from each of these segments.
            // Some user may prefer to work with the audio for fixing the transcript.
            // We will put the audio files in the same folder as the video.
            ExtractAll(splitFolder);

            // Split the full transcript into segments that match the audio and video segments in size.
            SplitTranscript splitTranscript = new SplitTranscript();
            splitTranscript.Split(fixasr, splitFolder, segmentSize, segmentOverlap, parts);

        }

        public bool CheckIfFinished(string meetingFolder)
        {
            return false;
        }

        public void Combine(string meetingFolder, string combinedFile)
        {

        }

        /* Extract the audio from mp4 files in subfolders of specified folder.
         * In the "Fix" folder for a recording, there will be a subfolder for each
         * segment of the recording: 00-03-00, 00-06-00, 00=09-00, etc.
         * Each of these subfolders is initialized with three files:
         *    "ToFix.mp4"  - the video of this segment
         *    "ToFix.flac" - the audio of this segment
         *    "ToFix.json" - the transcription of this segment
         */
        public void ExtractAll(string inputFolder)
        {
            foreach (string dir in Directory.GetDirectories(inputFolder))
            {
                string inputFile = Path.Combine(dir,"ToFix.mp4");
                // TODO - convert to mp3 instead of flac.
                string outputFile = Path.Combine(dir,"ToFix.flac");

                audioProcessing.Extract(inputFile, outputFile);
            }
        }

    }
}
