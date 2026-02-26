@echo off
sqlcmd -S . -d Robot -U sa -P 1 -i "C:\iko\robot\exec_arsiv.sql"