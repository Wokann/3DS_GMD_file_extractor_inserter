# 3DS_GMD_file_extractor_inserter
GMD file (text message) extractor/inserter for Megami Meguri (3DS) By Wokann.<br><br>
This tool is made based on the source codes from<br>
**"Phoenix Wright - Dual Destinies (3DS) GMD file extractor/inserter" By Skye.**<br>

## install
Here is one method to compile:
Find your Net framework address in C disk, like:<br>
`C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc`

Then open cmd or powershell in where Program.cs is, and insert:<br>
`C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc Program.cs`

You will get **Program.exe**.

## usage
To extract text from GMD:<br>
`Program -e file1 [file2 ...]`<br>
Extracted files will have extension .txt
    
To convert previously extracted text files back to GMD:<br>
`Program -i file1 [file2 ...]`<br>
Inserted files will have extension .newgmd

or you can input in powershell like these:<br>
`./Program.exe -e D:\xxx\\Credit_jpn.gmd`<br>
you will get Credit_jpn.txt.<br>
`./Program.exe -i D:\xxx\\Credit_jpn.txt`<br>
you will get Credit_jpn.newgmd.<br>

## warning
Remember if you want to use this tool on other games,<br>
you should make sure whether **the format of your .gmd files** is the same as files in Megami Meguri (3DS) or not.<br>
If not, not only you need to change the source codes to fit your format,<br>
but also you need to find **the two keys to encrypt/decrypt text** in your game.<br>


