﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;


namespace gmd
{
    class Program
    {
        static void Main(string[] args)
        {
            Title();
            int argc = args.Length;

            if (argc == 0)
            {
                Usage();
                return;
            }

            if (argc < 2)
            {
                Console.WriteLine("Not enough arguments.");
                Usage();
                return;
            }
            else
            {
                string mode = args[0];
                int f = 1;
                if (mode == "-e")
                {
                    for (; f < argc; f++)
                    {
                        Console.WriteLine(" - Extracting " + args[f]);
                        Extract(args[f]);
                    }
                }
                else if (mode == "-i")
                {
                    for (; f < argc; f++)
                    {
                        Console.WriteLine(" - Inserting " + args[f]);
                        Insert(args[f]);
                    }
                }
                else
                {
                    Console.WriteLine("Mode not recognized.");
                    Usage();
                }
            }
        }

        static void Title()
        {            
            Console.WriteLine("Megami Meguri (3DS) GMD file extractor/inserter By Wokann");
            Console.WriteLine("Based on \"Phoenix Wright - Dual Destinies (3DS) GMD file extractor/inserter\" By Skye");
        }

        static void Usage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine();
            Console.WriteLine("To extract text from GMD:");
            Console.WriteLine("   gmd -e file1 [file2 ...]");
            Console.WriteLine("   Extracted files will have extension .txt");
            Console.WriteLine();
            Console.WriteLine("To convert previously extracted text files back to GMD:");
            Console.WriteLine("   gmd -i file1 [file2 ...]");
            Console.WriteLine("   Inserted files will have extension .newgmd");
            Console.WriteLine();
        }

        static void Extract(string filename)
        {
            // encoding info at https://msdn.microsoft.com/en-us/library/system.text.encodinginfo%28v=vs.110%29.aspx
            System.Text.Encoding enc = System.Text.Encoding.GetEncoding("utf-8");
            System.Text.Decoder dec = enc.GetDecoder();

            using (BinaryReader br = new BinaryReader(File.Open(filename, FileMode.Open)))
            {
                string magic = new string(br.ReadChars(4));
                if (magic.Contains("GMD"))
                {
                    uint GMDversion = br.ReadUInt32();
                    uint language = br.ReadUInt32();
                    uint unknown = br.ReadUInt32();

                    br.ReadBytes(0x4);
                    uint labelnums = br.ReadUInt32();
                    uint textnums = br.ReadUInt32();
                    uint labelsize = br.ReadUInt32();

                    uint textdatasize = br.ReadUInt32();
                    int namelength = br.ReadInt32();
                    string name = new string(br.ReadChars(namelength));
                    br.ReadByte();

                    uint[] labelids = new uint[labelnums];
                    uint[] labeloffs1 = new uint[textnums];
                    uint[] labeloffs2 = new uint[textnums];
                    uint[] labeloffs3 = new uint[textnums];
                    uint[] labeloffs4 = new uint[textnums];

                    uint OwnLabelnums = 0;
                    for (uint i = 0; i < textnums; i++)
                    {
                        uint labelid = br.ReadUInt32();
                        if (labelid != i)
                        {
                            while (i != labelid)
                            {
                                labeloffs1[i] = 0x0;
                                labeloffs2[i] = 0x0;
                                labeloffs3[i] = 0x0;
                                labeloffs4[i] = 0x0;
                                i++;
                            }
                        }
                        labeloffs1[labelid] = br.ReadUInt32();
                        labeloffs2[labelid] = br.ReadUInt32();
                        labeloffs3[labelid] = br.ReadUInt32();
                        labeloffs4[labelid] = br.ReadUInt32();
                        OwnLabelnums++;
                        if(OwnLabelnums == labelnums)
                        {
                            i++;
                            while (i != textnums)
                            {
                                labeloffs1[i] = 0x0;
                                labeloffs2[i] = 0x0;
                                labeloffs3[i] = 0x0;
                                labeloffs4[i] = 0x0;
                                i++;
                            }
                            break;
                        }
                    }

                    uint[] dummy = new uint[0x100];
                    for(int i = 0; i < 0x100; i++)
                    {
                        dummy[i] = br.ReadUInt32();
                    }

                    string[] labels = new string[textnums];

                    for (int i = 0; i < textnums; i++)
                    {
                        if (labeloffs1[i] == 0x0)
                        {
                            labels[i] = "NO_LABEL";
                        }
                        else
                        {
                            labels[i] = readUntil(br, 0x00);
                        }
                    }

                    byte[] ciphertext = br.ReadBytes(Convert.ToInt32(textdatasize));
                    byte[] plaintext = xor_cipher(ciphertext);

                    using (StreamWriter sw = new StreamWriter(File.Create(filename.Replace(".gmd", ".txt")), enc))
                    {
                        sw.WriteLine("{GMDversion   :" + Convert.ToString(GMDversion)   + "}");
                        sw.WriteLine("{language     :" + Convert.ToString(language)     + "}");
                        sw.WriteLine("{unknown      :" + Convert.ToString(unknown)      + "}");
                        sw.WriteLine("{labelnums    :" + Convert.ToString(labelnums)    + "}");
                        sw.WriteLine("{textnums     :" + Convert.ToString(textnums)     + "}");
                        sw.WriteLine("{labelsize    :" + Convert.ToString(labelsize)    + "}");
                        sw.WriteLine("{textdatasize :" + Convert.ToString(textdatasize) + "}");
                        sw.WriteLine("{name         :" + name                           + "}");
                        sw.WriteLine();

                        int idx = 0;
                        for (int i = 0; i < textnums; i++)
                        {
                            int size = 0;
                            int start = idx;
                            sw.WriteLine("[" + Convert.ToString(i) 
                                       + ":" + labels[i]
                                       + ":" + Convert.ToString(labeloffs1[i]) 
                                       + ":" + Convert.ToString(labeloffs2[i]) 
                                       + ":" + Convert.ToString(labeloffs3[i]) 
                                       + ":" + Convert.ToString(labeloffs4[i]) + "]");
                            while (plaintext[idx] != 0x00)
                            {
                                size += 1;
                                idx += 1;
                            }
                            char[] buf = enc.GetChars(plaintext, start, size);
                            sw.Write(buf);
                            idx += 1;
                            sw.WriteLine("{end}");
                            sw.WriteLine();
                        }

                        sw.WriteLine("{dummy}");
                        for (int i = 0; i < 0x100; i++)
                        {
                            sw.WriteLine("[" + Convert.ToString(i*4) 
                                       + ":" + Convert.ToString(dummy[i++]) 
                                       + ":" + Convert.ToString(dummy[i++]) 
                                       + ":" + Convert.ToString(dummy[i++]) 
                                       + ":" + Convert.ToString(dummy[i]) + "]");
                        }
                        sw.WriteLine();
                    }
                }
                else
                {
                    Console.WriteLine("File is not GMD.");
                }
            }
        }

        static void Insert(string filename)
        {
            // encoding info at https://msdn.microsoft.com/en-us/library/system.text.encodinginfo%28v=vs.110%29.aspx

            System.Text.Encoding enc = System.Text.Encoding.GetEncoding("utf-8");

            UInt32 initialGMDversion = 0;
            UInt32 initiallanguage = 0;
            uint unknown = 0;
            uint labelnum = 0;
            uint textnum = 0;
            string name = "";

            List<uint> idList = new List<uint>();
            List<string> labelList = new List<string>();
            List<uint> labeloffs1List = new List<uint>();
            List<uint> labeloffs2List = new List<uint>();
            List<uint> labeloffs3List = new List<uint>();
            List<uint> labeloffs4List = new List<uint>();
            List<uint> dummyList = new List<uint>();

            byte[] plaintext;
            byte[] newline = new byte[] { 0xa };
            byte[] newpage = new byte[] { 0xd, 0xa };

            using (MemoryStream textStream = new MemoryStream())
            {
                using (StreamReader sr = new StreamReader(File.OpenRead(filename), enc))
                {
                    // read the initial GMDversion
                    string GMDversion = sr.ReadLine();
                    if (GMDversion.Contains("{") && GMDversion.Contains("}"))
                        initialGMDversion = Convert.ToUInt32(GMDversion.Substring(15, GMDversion.Length - 16));
                    else
                    {
                        Console.WriteLine("Expected initial GMDversion in line ");
                        Console.WriteLine(GMDversion);
                        return;
                    }

                    // read the initial language
                    string language = sr.ReadLine();
                    if (language.Contains("{") && language.Contains("}"))
                        initiallanguage = Convert.ToUInt32(language.Substring(15, language.Length - 16));
                    else
                    {
                        Console.WriteLine("Expected initial language in line ");
                        Console.WriteLine(language);
                        return;
                    }

                    // read the unknown value
                    string unk = sr.ReadLine();
                    if (unk.Contains("{") && unk.Contains("}"))
                    {
                        unknown = Convert.ToUInt32(unk.Substring(15, unk.Length - 16));
                        sr.ReadLine();
                        sr.ReadLine();
                        sr.ReadLine();
                        sr.ReadLine();
                    }
                    else
                    {
                        Console.WriteLine("Expected header value 'unknown' in line ");
                        Console.WriteLine(unk);
                        return;
                    }

                    // read the internal file name
                    name = sr.ReadLine();
                    if (name.Contains("{") && name.Contains("}"))
                    {
                        name = name.Substring(15, name.Length - 16);
                        sr.ReadLine();
                    }
                    else
                    {
                        Console.WriteLine("Expected internal file name in line ");
                        Console.WriteLine(name);
                        return;
                    }

                    while (!sr.EndOfStream)
                    {
                        // begin reading file
                        string line = sr.ReadLine();
                        if (line.Contains("[") && line.Contains("]"))
                        {
                            // get the id and label
                            line = line.Substring(1, line.Length - 2);
                            uint id = Convert.ToUInt32(line.Split(':')[0]);
                            string lbl = line.Split(':')[1];
                            UInt32 labeloffs1 = Convert.ToUInt32(line.Split(':')[2]);
                            UInt32 labeloffs2 = Convert.ToUInt32(line.Split(':')[3]);
                            UInt32 labeloffs3 = Convert.ToUInt32(line.Split(':')[4]);
                            UInt32 labeloffs4 = Convert.ToUInt32(line.Split(':')[5]);
                            if (lbl != "NO_LABEL")
                            {
                                idList.Add(id);
                                labelList.Add(lbl);
                                labeloffs1List.Add(labeloffs1);
                                labeloffs2List.Add(labeloffs2);
                                labeloffs3List.Add(labeloffs3);
                                labeloffs4List.Add(labeloffs4);
                                labelnum += 1;
                            }                                    
                            textnum = id;
                            line = sr.ReadLine();
                            // then the text
                            while (!line.Contains("{end}"))
                            {
                                byte[] buf = enc.GetBytes(line);
                                textStream.Write(buf, 0, buf.Length);
                                textStream.Write(newpage, 0, 2);
                                line = sr.ReadLine();
                            }
                            // reached {end} line
                            line = line.Substring(0, line.Length - 5);
                            byte[] buf2 = enc.GetBytes(line);
                            textStream.Write(buf2, 0, buf2.Length);
                            textStream.WriteByte(Convert.ToByte(0x00));
                            sr.ReadLine();
                        }
                        //dummy 
                        else if(line.Contains("{dummy}"))
                        {
                            for (int i = 0; i < 0x40; i++)
                            {
                                line = sr.ReadLine();
                                line = line.Substring(1, line.Length - 2);
                                uint dummyid = Convert.ToUInt32(line.Split(':')[0]);
                                UInt32 dummyoffs1 = Convert.ToUInt32(line.Split(':')[1]);
                                UInt32 dummyoffs2 = Convert.ToUInt32(line.Split(':')[2]);
                                UInt32 dummyoffs3 = Convert.ToUInt32(line.Split(':')[3]);
                                UInt32 dummyoffs4 = Convert.ToUInt32(line.Split(':')[4]);
                                dummyList.Add(dummyoffs1);
                                dummyList.Add(dummyoffs2);
                                dummyList.Add(dummyoffs3);
                                dummyList.Add(dummyoffs4);
                            }
                            sr.ReadLine();
                        }
                        else
                        {
                            Console.WriteLine("Expected label in line ");
                            Console.WriteLine(line);
                            return;
                        }
                    }
                    // all the file has been read, time to mash into shape
                    plaintext = textStream.ToArray();
                    byte[] ciphertext = xor_cipher(plaintext);
                    //byte[] ciphertext = plaintext;
                    UInt32 textdatasize = Convert.ToUInt32(ciphertext.Length);
                    string[] labelStringArray = labelList.ToArray();
                    UInt32 labelsize = 0;
                    foreach (string label in labelList)
                    {
                        labelsize += Convert.ToUInt32(label.Length + 1);
                    }

                    using (BinaryWriter bw = new BinaryWriter(File.Open(filename.Replace(".txt", ".newgmd"), FileMode.Create)))
                    {
                        // header
                        bw.Write(0x00444D47); // GMD\0
                        bw.Write(initialGMDversion); // unknown
                        bw.Write(initiallanguage); // eng = 0x1, jap = 0x0
                        bw.Write(unknown); // unknown
                        bw.Write(0x0); // unknown
                        bw.Write(labelnum);
                        bw.Write(textnum+1);
                        bw.Write(labelsize);
                        bw.Write(textdatasize);
                        bw.Write(name.Length);
                        bw.Write(name.ToCharArray());
                        bw.Write(Convert.ToByte(0x00));
                        // begin pointer table
                        for (int i = 0; i < labelnum; i++)
                        {
                            bw.Write(idList[i]);
                            bw.Write(labeloffs1List[i]);
                            bw.Write(labeloffs2List[i]);
                            bw.Write(labeloffs3List[i]);
                            bw.Write(labeloffs4List[i]);
                        }
                        // begin dummy
                        for (int i = 0; i < 0x100; i++)
                        {
                            bw.Write(dummyList[i]);
                        }
                        // begin labels
                        for (int i = 0; i < labelnum; i++)
                        {
                            string l = labelStringArray[i];
                            bw.Write(enc.GetBytes(labelStringArray[i]));
                            bw.Write(Convert.ToByte(0x00));
                        }
                        // write ciphertext
                        bw.Write(ciphertext);
                    }
                }
            }
        }

        static byte[] xor_cipher(byte[] data)
        {
            string key1 = "hbaleoha8yjh493lfjhh2oft;prjhgi3";
            string key2 = "q@prkgiu4h05kgjnbh4llpkt;2;fihit";

            for (int i = 0; i < data.Length; i++)
            {
                char x1 = key1[i % 32];
                char x2 = key2[i % 32];

                data[i] = Convert.ToByte(data[i] ^ x1 ^ x2);
            }
            return data;
        }

        static string readUntil(BinaryReader br, byte stop)
        {
            string buf = "";
            if (br.BaseStream.Position == br.BaseStream.Length)
            {
                return buf;
            }
            byte b = br.ReadByte();
            while ((b != stop) && (br.BaseStream.Position != br.BaseStream.Length))
            {
                buf += Convert.ToChar(b);
                b = br.ReadByte();
            }
            return buf;
        }
    }
}
