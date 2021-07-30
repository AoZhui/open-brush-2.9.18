﻿/*
LIGHTNING ARTIST TOOLKIT v1.1.0

The Lightning Artist Toolkit was developed with support from:
   Canada Council on the Arts
   Eyebeam Art + Technology Center
   Ontario Arts Council
   Toronto Arts Council
   
Copyright (c) 2021 Nick Fox-Gieg
http://fox-gieg.com

~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
# 
#     http://www.apache.org/licenses/LICENSE-2.0
# 
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.
*/

//#if LATK_SUPPORTED
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using SimpleJSON;
using ICSharpCode.SharpZipLibUnityPort.Core;
using ICSharpCode.SharpZipLibUnityPort.Zip;
using static TiltBrush.ExportUtils;

namespace TiltBrush
{

    public class ExportLatk
    {

        private string writeFileName = "test.zip";
        private bool writePressure = true;
        private bool useTimestamp = true;
        private Vector2 brushSizeRange = new Vector2(0f, 1f);
        private float consoleUpdateInterval = 0f;
        private int layerNum = 1;
        private int frameNum = 1;

        private void getBrushSizeRange()
        {
            List<float> allBrushSizes = new List<float>();

            for (int i = 0; i < SketchMemoryScript.m_Instance.StrokeCount; i++)
            {
                Stroke stroke = SketchMemoryScript.m_Instance.GetStrokeAtIndex(i);
                allBrushSizes.Add(stroke.m_BrushSize);
            }

            allBrushSizes.Sort();
            brushSizeRange = new Vector2(allBrushSizes[0], allBrushSizes[allBrushSizes.Count - 1]);
        }

        private float getNormalizedBrushSize(float s)
        {
            return map(s, brushSizeRange.x, brushSizeRange.y, 0.1f, 1f);
        }

        private float map(float s, float a1, float a2, float b1, float b2)
        {
            return b1 + (s - a1) * (b2 - b1) / (a2 - a1);
        }

        public IEnumerator writeLatkStrokes()
        {
            Debug.Log("*** Begin writing...");

            string ext = Path.GetExtension(writeFileName).ToLower();
            Debug.Log("Found extension " + ext);
            bool useZip = (ext == ".latk" || ext == ".zip");

            List<string> FINAL_LAYER_LIST = new List<string>();

            if (writePressure) getBrushSizeRange();

            for (int hh = 0; hh < layerNum; hh++)
            {
                int currentLayer = hh;

                List<string> sb = new List<string>();
                List<string> sbHeader = new List<string>();
                sbHeader.Add("\t\t\t\t\t\"frames\":[");
                sb.Add(string.Join("\n", sbHeader.ToArray()));

                for (int h = 0; h < frameNum; h++)
                {
                    int currentFrame = h;

                    List<string> sbbHeader = new List<string>();
                    sbbHeader.Add("\t\t\t\t\t\t{");
                    sbbHeader.Add("\t\t\t\t\t\t\t\"strokes\":[");
                    sb.Add(string.Join("\n", sbbHeader.ToArray()));
                    for (int i = 0; i < SketchMemoryScript.m_Instance.StrokeCount; i++)
                    {
                        Stroke stroke = SketchMemoryScript.m_Instance.GetStrokeAtIndex(i);

                        List<string> sbb = new List<string>();
                        sbb.Add("\t\t\t\t\t\t\t\t{");
                        float r = stroke.m_Color.r;
                        float g = stroke.m_Color.g;
                        float b = stroke.m_Color.b;
                        sbb.Add("\t\t\t\t\t\t\t\t\t\"color\":[" + r + ", " + g + ", " + b + "],");

                        if (stroke.m_ControlPoints.Length > 0)
                        {
                            sbb.Add("\t\t\t\t\t\t\t\t\t\"points\":[");
                            for (int j = 0; j < stroke.m_ControlPoints.Length; j++)
                            {
                                PointerManager.ControlPoint point = stroke.m_ControlPoints[j];
                                float x = point.m_Pos.x;
                                float y = point.m_Pos.y;
                                float z = point.m_Pos.z;

                                float pressureVal = 1f;
                                if (writePressure) pressureVal = point.m_Pressure * getNormalizedBrushSize(stroke.m_BrushSize);

                                if (j == stroke.m_ControlPoints.Length - 1)
                                {
                                    sbb.Add("\t\t\t\t\t\t\t\t\t\t{\"co\":[" + x + ", " + y + ", " + z + "], \"pressure\":" + pressureVal + ", \"strength\":1}");
                                    sbb.Add("\t\t\t\t\t\t\t\t\t]");
                                }
                                else
                                {
                                    sbb.Add("\t\t\t\t\t\t\t\t\t\t{\"co\":[" + x + ", " + y + ", " + z + "], \"pressure\":" + pressureVal + ", \"strength\":1},");
                                }
                            }
                        }
                        else
                        {
                            sbb.Add("\t\t\t\t\t\t\t\t\t\"points\":[]");
                        }

                        if (i == SketchMemoryScript.m_Instance.StrokeCount - 1)
                        {
                            sbb.Add("\t\t\t\t\t\t\t\t}");
                        }
                        else
                        {
                            sbb.Add("\t\t\t\t\t\t\t\t},");
                        }

                        sb.Add(string.Join("\n", sbb.ToArray()));
                    }

                    yield return new WaitForSeconds(consoleUpdateInterval);

                    List<string> sbFooter = new List<string>();
                    if (h == frameNum - 1)
                    {
                        sbFooter.Add("\t\t\t\t\t\t\t]");
                        sbFooter.Add("\t\t\t\t\t\t}");
                    }
                    else
                    {
                        sbFooter.Add("\t\t\t\t\t\t\t]");
                        sbFooter.Add("\t\t\t\t\t\t},");
                    }
                    sb.Add(string.Join("\n", sbFooter.ToArray()));
                }

                FINAL_LAYER_LIST.Add(string.Join("\n", sb.ToArray()));
            }

            yield return new WaitForSeconds(consoleUpdateInterval);
            Debug.Log("+++ Parsing finished. Begin file writing.");
            yield return new WaitForSeconds(consoleUpdateInterval);

            List<string> s = new List<string>();
            s.Add("{");
            s.Add("\t\"creator\": \"unity\",");
            s.Add("\t\"grease_pencil\":[");
            s.Add("\t\t{");
            s.Add("\t\t\t\"layers\":[");

            for (int i = 0; i < layerNum; i++)
            {
                int currentLayer = i;

                s.Add("\t\t\t\t{");
                {
                    s.Add("\t\t\t\t\t\"name\": \"OpenBrushLayer " + (currentLayer + 1) + "\",");
                }

                s.Add(FINAL_LAYER_LIST[currentLayer]);

                s.Add("\t\t\t\t\t]");
                if (currentLayer < layerNum - 1)
                {
                    s.Add("\t\t\t\t},");
                }
                else
                {
                    s.Add("\t\t\t\t}");
                }
            }
            s.Add("            ]"); // end layers
            s.Add("        }");
            s.Add("    ]");
            s.Add("}");

            string url = "";
            string tempName = "";
            if (useTimestamp)
            {
                string extO = "";
                if (useZip)
                {
                    extO = ".latk";
                }
                else
                {
                    extO = ".json";
                }
                tempName = writeFileName.Replace(extO, "");
                int timestamp = (int)(System.DateTime.UtcNow.Subtract(new System.DateTime(1970, 1, 1))).TotalSeconds;
                tempName += "_" + timestamp + extO;
            }

            url = Path.Combine(Application.dataPath, tempName);

#if UNITY_ANDROID
        //url = "/sdcard/Movies/" + tempName;
        url = Path.Combine(Application.persistentDataPath, tempName);
#endif

#if UNITY_IOS
		url = Path.Combine(Application.persistentDataPath, tempName);
#endif

            if (useZip)
            {
                saveJsonAsZip(url, tempName, string.Join("\n", s.ToArray()));
            }
            else
            {
                File.WriteAllText(url, string.Join("\n", s.ToArray()));
            }

            Debug.Log("*** Wrote " + url);

            yield return null;
        }

        JSONNode getJsonFromZip(byte[] bytes)
        {
            // https://gist.github.com/r2d2rigo/2bd3a1cafcee8995374f

            MemoryStream fileStream = new MemoryStream(bytes, 0, bytes.Length);
            ZipFile zipFile = new ZipFile(fileStream);

            foreach (ZipEntry entry in zipFile)
            {
                if (Path.GetExtension(entry.Name).ToLower() == ".json")
                {
                    Stream zippedStream = zipFile.GetInputStream(entry);
                    StreamReader read = new StreamReader(zippedStream, true);
                    string json = read.ReadToEnd();
                    Debug.Log(json);
                    return JSON.Parse(json);
                }
            }

            return null;
        }

        void saveJsonAsZip(string url, string fileName, string s)
        {
            // https://stackoverflow.com/questions/1879395/how-do-i-generate-a-stream-from-a-string
            // https://github.com/icsharpcode/SharpZipLib/wiki/Zip-Samples
            // https://stackoverflow.com/questions/8624071/save-and-load-memorystream-to-from-a-file

            MemoryStream memStreamIn = new MemoryStream();
            StreamWriter writer = new StreamWriter(memStreamIn);
            writer.Write(s);
            writer.Flush();
            memStreamIn.Position = 0;

            MemoryStream outputMemStream = new MemoryStream();
            ZipOutputStream zipStream = new ZipOutputStream(outputMemStream);

            zipStream.SetLevel(3); //0-9, 9 being the highest level of compression

            string fileNameMinusExtension = "";
            string[] nameTemp = fileName.Split('.');
            for (int i = 0; i < nameTemp.Length - 1; i++)
            {
                fileNameMinusExtension += nameTemp[i];
            }

            ZipEntry newEntry = new ZipEntry(fileNameMinusExtension + ".json");
            newEntry.DateTime = System.DateTime.Now;

            zipStream.PutNextEntry(newEntry);

            StreamUtils.Copy(memStreamIn, zipStream, new byte[4096]);
            zipStream.CloseEntry();

            zipStream.IsStreamOwner = false;    // False stops the Close also Closing the underlying stream.
            zipStream.Close();          // Must finish the ZipOutputStream before using outputMemStream.

            outputMemStream.Position = 0;

            using (FileStream file = new FileStream(url, FileMode.Create, System.IO.FileAccess.Write))
            {
                byte[] bytes = new byte[outputMemStream.Length];
                outputMemStream.Read(bytes, 0, (int)outputMemStream.Length);
                file.Write(bytes, 0, bytes.Length);
                outputMemStream.Close();
            }

            /*
            // Alternative outputs:
            // ToArray is the cleaner and easiest to use correctly with the penalty of duplicating allocated memory.
            byte[] byteArrayOut = outputMemStream.ToArray();

            // GetBuffer returns a raw buffer raw and so you need to account for the true length yourself.
            byte[] byteArrayOut = outputMemStream.GetBuffer();
            long len = outputMemStream.Length;
            */
        }

    }

}
//#endif