# Tesseract CLI and Nuget Package
Tesseract cli wrapper for windows and linux

## Getting Started

Download latest [Binary](https://github.com/balbarak/tesseract/releases/download/v1.0.0/tsct.exe) from releases or build from source using visual studio 2022 and dotnet core 6

## Windows CLI Usage

To display options

    tsct.exe --help 

To read image data from file and output the result in terminal (supported image based on tesseract)

    tsct.exe --input "path/to/image.jpg"

To read image data from file and output to text file

    tsct.exe --input "path/to/image.jpg" --output "path/to/output.txt"

To execute Tesseract raw command

    tsct.exe --action raw "path/to/image.jpg - --psm 6 -c preserve_interword_spaces=1"


## Linux CLI Usage

Comming soon ....