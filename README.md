# Textract geometry annotation tool (invoice processing)

Textract geometry uploads a PDF to AWS S3 storage, then uses the Textract API 
to analyse the document and creates an annotated PDF which shows the bounding 
boxes in red for LINE elements which were found by Textract analysis.

Also a JSON output file is generated which contains all block information as 
recognized by Textract. This JSON file is compatible with the JSON output 
generated when using the AWS CLI.

The tool is merely intented to visualize the JSON output from the Textract 
command line interface such that invoice processing applications can be 
created more quickly.

## Support

This project is maintained by Inmote. For support you can email to info@inmote.com
and refer to the contact details on https://www.inmote.nl.

## Features

- Windows .NET 4.7.2 Framework console application written in C#.
- Configure AWS account details in TextractGeometry.xml (bucket name, access key, secret).
- Upload PDF to AWS S3 storage.
- Use Textract API to perform document analysis.
- Wait for the job to complete and receive a list of blocks.
- Create a PDF with the original content plus red rectangles add which shows 
the bounding boxes for each LINE element that was recognized by the Textract API. 
- Creates a JSON output file with all recognized block information from Textract.

## Setup

No setup required. Download the ZIP file from "Releases" and copy the contents
into a folder.

## Usage

TextractGeometry <PDF file> <Output PDF file> 
Output a PDF annotated with the bounding boxes in red as found by AWS Textract.
Also generates a JSON output file with the same file name as <Output PDF file>,
but the extension replaced by ".json".

Example: TextractGeometry invoice.pdf annotated_invoice.pdf
will generate annotated_invoice.pdf and annotated_invoice.json 
using invoice.pdf as input.

## License
MIT

