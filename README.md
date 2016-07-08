#CloudArchive

**Copyright (c) 2015-2016 David Betz**

Tool to mirror local branches to Azure or S3.

## Config

First, you create storage config (with keys). Then, you create "areas" with a name (simply) for output, a folder (the branch you want to mirror), what storage config you want (created from previous step), what file types you want to scan for (sometimes you'll want zip/rar to one storage system and images to a different one), and, optionally, what remote branch you want the entire thing put under.

Basically, do the following...

    storageAccounts:
        - name: cloudarchive01
          provider: azure
          key1: AZURE_KEY_HERE
          key2: azure only uses key 1
            
        - name: cloudarchive02
          provider: s3
          key1: S3_ACCESS_KEY_HERE
          key2: S3_ACCESS_SECRET_HERE
      
    areas:
      - name: book_images
        folder: C:\_BOOK
        container: books
        remoteBranch: images
        storage: cloudarchive01
        fileTypes:
            - extension: png
            - extension: svg
            - extension: jpg
            - extension: jpeg
            - extension: gif
              
      - name: book_text
        folder: C:\_BOOK
        container: books
        remoteBranch: text
        storage: cloudarchive01
        fileTypes:
            - extension: txt
      
      - name: mp3
        folder: C:\_AUDIOTEST
        container: mp3
        storage: cloudarchive01
        fileTypes:
            - extension: mp3
           
      - name: archives
        remoteBranch: misc
        container: s3 does not use container; use remoteBranch for a remote folder
        folder: C:\_ARCHIVETEST
        storage: cloudarchive02
        fileTypes:
            - extension: zip
            - extension: rar
            - extension: gz
          
In this example, I'm searching a book folder (personally, I scan and OCR most of my books), and putting the raw images in one place and the OCRed text files in another. A common senario might be to put images in photo album backups in one place and home videos in another. In the above scenario, they are in the same storage account, but in different folders, they could just as easily be put in different ones (hot storage and cool storage), or just in different containers (private and public or just two for the sake of two).

## Usage
        
You run this with this:

    carchive.exe -a <area_name>

But this will only do a dry run to show you what will happen. To run for real:

    carchive.exe -a <area_name> -l

Get itemized output with the -v option:

    carchive.exe -a <area_name> -l -v

Basically just look at the command line options.

## Optimization
        
A local .dates.json file is created to optimize uploads. The system will check hashes to see if the file changed. This also means updates are incremental (only new or updates files are uploaded). You can invalidate a single file by modifying it's entry in the file or you can do a full upload with the -f command line option.

Note: if you change the storage config, you'll have to run with -f or else the system will think nothing has changed. Which makes sense, because nothing has.

## Sensitive Keys

Because putting sensitive information in config files is stupid, you can put your key in a file and simply reference the file name in ().

Basically:

    storageAccounts:
        - name: cloudarchive01
          provider: azure
          key1: (H:\_ENCRYPTED_FOLDER\azure_key.txt)
          key2: azure only uses key 1
            
        - name: cloudarchive02
          provider: s3
          key1: (F:\_ENCRYPTED_FOLDER\s3_key1.txt)
          key2: (G:\_ENCRYPTED_FOLDER\s3_key2.txt)

