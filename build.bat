Set PATH=%PATH%;"C:\Program Files\7-Zip"
Set BUILD=%date:~4,2%_%date:~7,2%_%date:~10,4%
pause Press CRTL-C to quit now
del BDB_*.zip
7z a BDB_FULL_%BUILD%.zip @bdb_list_full.txt
pause