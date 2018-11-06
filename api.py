#!/usr/bin/python
# -*- coding: utf-8 -*-

import sys
import os
import cgi
import datetime
import base64
import subprocess

from datetime import datetime

REQUEST_HEAD_FUNC = "FUNC"
REQUEST_HEAD_TARGET = "TARGET"

REQUEST_TYPE_UNKNOWN = "UNKNOWN"
REQUEST_TYPE_STATUS = "STATUS"
REQUEST_TYPE_GET_SW = "GETSW"
REQUEST_TYPE_SET_SW = "SETSW"
REQUEST_TYPE_RUNONCE = "RUNONCE"
REQUEST_TYPE_GET_PIC = "GETPIC"

DATA_FIELD = "#DATA_-_FIELD#"

PROFILE_PATH_BASE = "/YOUR_PATH"
PROFILE_PATH_DEFAULT = PROFILE_PATH_BASE + "default.cron"
PROFILE_PATH_TV = PROFILE_PATH_BASE + "tv.cron"
PROFILE_PATH_AIRCON = PROFILE_PATH_BASE + "aircon.cron"
PROFILE_PATH_SYNC = PROFILE_PATH_BASE + "sync.cron"
PROFILE_PATH_TEMP = "/tmp/cron.temp"

RUNONCE_PATH_BASE = PROFILE_PATH_BASE
RUNONCE_PATH_TV = RUNONCE_PATH_BASE + "tv.sh"
RUNONCE_PATH_AIRCON = RUNONCE_PATH_BASE + "aircon.sh"
RUNONCE_PATH_SYNC = RUNONCE_PATH_BASE + "sync.sh"
RUNONCE_PATH_PIC = RUNONCE_PATH_BASE + "pic.sh"

WEBCAM_IP_ADDRESS = "YOUR WEBCAM IP"
WEBCAM_STREAM_URL = "rtsp://" + WEBCAM_IP_ADDRESS + "/unicast"
WEBCAM_CAPTURE_CMD = "sudo avconv -y -rtsp_transport tcp -i \"" + WEBCAM_STREAM_URL + "\" -q:v 9 -vframes 1 "

PICTURE_BASE_PATH = PROFILE_PATH_BASE + "pic/"
PICTURE_HEAD_STRING = "Webcam_"
PICTURE_EXT = ".jpg"

EXIST_MARK_TV = "#PiServer TV"
EXIST_MARK_AIRCON = "#PiServer Aircon"
EXIST_MARK_SYNC = "#PiServer Sync"

SW_TV_ON = "#SW_TV_ON#"
SW_AIRCON_ON = "#SW_AIRCON_ON#"
SW_SYNC_ON = "#SW_SYNC_ON#" 

SW_TV_OFF = "#SW_TV_OFF#"
SW_AIRCON_OFF = "#SW_AIRCON_OFF#"
SW_SYNC_OFF = "#SW_SYNC_OFF#"

RUNONCE_TV = "#RUNONCE_TV#"
RUNONCE_AIRCON = "#RUNONCE_AIRCON#"
RUNONCE_SYNC = "#RUNONCE_SYNC#"
RUNONCE_PIC = "#RUNONCE_PIC#"

RETURN_OK = "#RETURN_OK#"
RETURN_NG = "#RETURN_NG#"

cmdOut = ""
cmdErr = ""
cmdErrCode = 0

isDebug = 0

def RunCmd(inputCmd):
    global cmdOut, cmdErr, cmdErrCode
    process = subprocess.Popen(inputCmd, shell=True,
                               stdout=subprocess.PIPE, 
                               stderr=subprocess.PIPE)

    cmdOut, cmdErr = process.communicate()
    cmdErrCode = process.returncode

def TakePictureFromWebcam():
    reqTime = datetime.now().strftime("%Y_%m_%d_%H_%M_%S")
    localFilename = PICTURE_BASE_PATH + PICTURE_HEAD_STRING + reqTime + PICTURE_EXT

    RunCmd("ping " + WEBCAM_IP_ADDRESS + " -c 1 >/dev/null 2>&1")    
    if cmdErrCode is 0:
        print("Webcam Online.")
        finalCmd = WEBCAM_CAPTURE_CMD + localFilename

        if isDebug:
           print(">>> Debug: CMD: " + finalCmd)

        RunCmd(finalCmd)
        if cmdErrCode is 0:
            print("Take Picture Successed.")
            print(RETURN_OK)
            return DATA_FIELD + reqTime + DATA_FIELD
        else:
            print("Take Picture Failed.")
            return RETURN_NG
    else:
        print("Webcam Offline")
        return RETURN_NG


def DecodeMe(inputRaw):
    try:
        decode1st = base64.b64decode( inputRaw )
        return base64.b64decode( decode1st[1:] )
    except:
        return REQUEST_HEAD_FUNC + "=" + REQUEST_TYPE_UNKNOWN

if __name__ == '__main__':

    query_string = os.environ.get("QUERY_STRING")

    if not query_string:
        param = sys.argv[1]
        isDebug = 1
        print(">>> Debug: Encoded query string: " + param)
    else:
        param = query_string

    if not param:
        print(RETURN_NG)
        print("Get query string failed.")
    else:
        decodedQuery = DecodeMe( param )
 
        if isDebug:
           print(">>> Debug: Decoded query string: " + decodedQuery)

        queryPairs = cgi.parse_qs( decodedQuery )
        func = queryPairs.get(REQUEST_HEAD_FUNC, [None])[0]

        if isDebug:
           print(">>> Debug: Func: " + func)

        if func == REQUEST_TYPE_STATUS:
            print(RETURN_OK)
            RunCmd("uptime -p")
            print("Uptime: " + cmdOut.strip())
            RunCmd("uptime | head -1 | awk -F': ' '{print $2}'")
            print("Sys.Load: " + cmdOut.strip())
        elif func == REQUEST_TYPE_GET_SW:
            RunCmd("sudo crontab -l | grep '" + EXIST_MARK_TV + "'")
            if cmdErrCode is 0:
                print(SW_TV_ON)
            else:
                print(SW_TV_OFF)

            RunCmd("sudo crontab -l | grep '" + EXIST_MARK_AIRCON + "'")
            if cmdErrCode is 0:
                print(SW_AIRCON_ON)
            else:
                print(SW_AIRCON_OFF)

            RunCmd("sudo crontab -l | grep '" + EXIST_MARK_SYNC + "'")
            if cmdErrCode is 0:
                print(SW_SYNC_ON)
            else:
                print(SW_SYNC_OFF)

            print(RETURN_OK)

        elif func == REQUEST_TYPE_SET_SW:
            target = queryPairs.get(REQUEST_HEAD_TARGET, [None])[0]

            RunCmd("sudo rm -rf " + PROFILE_PATH_TEMP)
            RunCmd("echo '# piServer Auto Script' > " + PROFILE_PATH_TEMP)
            RunCmd("echo >> " + PROFILE_PATH_TEMP)
            RunCmd("cat " + PROFILE_PATH_DEFAULT + " >> " + PROFILE_PATH_TEMP)
            RunCmd("echo >> " + PROFILE_PATH_TEMP)

            if target == SW_TV_ON:
                print("Turn On Auto On/Off TV.")
                RunCmd("cat " + PROFILE_PATH_TV + " >> " + PROFILE_PATH_TEMP)
            elif target == SW_TV_OFF:
                print("Turn Off Auto On/Off TV.")
                RunCmd("echo '" + SW_TV_OFF + "' >> " + PROFILE_PATH_TEMP)
            else:
                RunCmd("sudo crontab -l | grep '" + EXIST_MARK_TV + "'")
                if cmdErrCode is 0:
                    RunCmd("cat " + PROFILE_PATH_TV + " >> " + PROFILE_PATH_TEMP)
            RunCmd("echo >> " + PROFILE_PATH_TEMP)

            if target == SW_AIRCON_ON:
                print("Turn On Auto On/Off Aircon.")
                RunCmd("cat " + PROFILE_PATH_AIRCON + " >> " + PROFILE_PATH_TEMP)
            elif target == SW_AIRCON_OFF:
                print("Turn Off Auto On/Off Aircon.")
                RunCmd("echo '" + SW_AIRCON_OFF + "' >> " + PROFILE_PATH_TEMP)
            else:
                RunCmd("sudo crontab -l | grep '" + EXIST_MARK_AIRCON + "'")
                if cmdErrCode is 0:
                    RunCmd("cat " + PROFILE_PATH_AIRCON + " >> " + PROFILE_PATH_TEMP)
            RunCmd("echo >> " + PROFILE_PATH_TEMP)
            
            if target == SW_SYNC_ON:
                print("Turn On Auto Sync With Server.")
                RunCmd("cat " + PROFILE_PATH_SYNC + " >> " + PROFILE_PATH_TEMP)
            elif target == SW_SYNC_OFF:
                print("Turn Off Auto Sync With Server.")
                RunCmd("echo '" + SW_SYNC_OFF + "' >> " + PROFILE_PATH_TEMP)
            else:
                RunCmd("sudo crontab -l | grep '" + EXIST_MARK_SYNC + "'")
                if cmdErrCode is 0:
                    RunCmd("cat " + PROFILE_PATH_SYNC + " >> " + PROFILE_PATH_TEMP)
            RunCmd("echo >> " + PROFILE_PATH_TEMP)

            RunCmd("sudo crontab " + PROFILE_PATH_TEMP)
            if cmdErrCode is 0:
                print("Operation Completed.")
                print(RETURN_OK)
            else:
                print("Operation Failed.")
                print(RETURN_NG)

            RunCmd("sudo rm " + PROFILE_PATH_TEMP)

        elif func == REQUEST_TYPE_RUNONCE:
            target = queryPairs.get(REQUEST_HEAD_TARGET, [None])[0]
            
            if target == RUNONCE_TV:
                print("Run TV Script.")
                RunCmd("sudo " + RUNONCE_PATH_TV + "&")
                print("Operation Finished.")
                print(RETURN_OK)
            if target == RUNONCE_AIRCON:
                print("Run Aircon Script.")
                RunCmd("sudo " + RUNONCE_PATH_AIRCON + "&")
                print("Operation Finished.")
                print(RETURN_OK)
            if target == RUNONCE_SYNC:
                print("Run Sync Script.")
                RunCmd("sudo " + RUNONCE_PATH_SYNC + "&")
                print("Operation Finished.")
                print(RETURN_OK)
            if target == RUNONCE_PIC:
                print("Take Picture From Camera.")
                print( TakePictureFromWebcam() )
        elif func == REQUEST_TYPE_GET_PIC:
            target = queryPairs.get(REQUEST_HEAD_TARGET, [None])[0]

            localFilePath = PICTURE_BASE_PATH + PICTURE_HEAD_STRING + target + PICTURE_EXT

            sys.stdout.write("Content-Type: image/jpg\n")
            sys.stdout.write("Content-Length: " + str(os.stat(localFilePath).st_size) + "\n")
            sys.stdout.write("\n")
            sys.stdout.flush()
            sys.stdout.write( open(localFilePath, "rb").read() )
	elif func == REQUEST_TYPE_UNKNOWN:
            print(RETURN_NG)
            print("Auth Failed - 1")
        else:
            print(RETURN_NG)
            print("Auth Failed - 2")
