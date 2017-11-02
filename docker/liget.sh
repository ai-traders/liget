#!/bin/bash
set -e

###################################
####### Setup loggging ############

liget_log4net_file='/etc/liget/log4net.xml'

if [[ $LIGET_LOG_BACKEND == "custom" ]]; then
  if [ ! -f "$liget_log4net_file" ]; then
    echo "Log4net config file $liget_log4net_file does not exist, expected to be setup by user because LIGET_LOG_BACKEND=$LIGET_LOG_BACKEND"
    exit 1;
  fi
elif [[ $LIGET_LOG_BACKEND == "console" ]]; then
  cat << EOF > $liget_log4net_file
<log4net>
  <appender name="ConsoleAppender" type="log4net.Appender.ConsoleAppender">
    <layout type="log4net.Layout.PatternLayout">
      <conversionPattern value="%date [%thread] %-5level %logger - %message%newline" />
    </layout>
  </appender>
  <root>
    <level value="${LIGET_LOG_LEVEL}" />
    <appender-ref ref="ConsoleAppender" />
  </root>
</log4net>
EOF
elif [[ $LIGET_LOG_BACKEND == "gelf" ]]; then
  if [ -z "$LIGET_LOG_GELF_HOST" ]; then
    echo "LIGET_LOG_GELF_HOST is not set";
    exit 1;
  fi
  cat << EOF > $liget_log4net_file
<log4net>
  <appender name="GelfUdpAppender" type="Gelf4net.Appender.GelfUdpAppender, Gelf4Net.UdpAppender">
	  <remoteHostName value="$LIGET_LOG_GELF_HOST"/>
	  <remotePort value="$LIGET_LOG_GELF_PORT" />
	  <layout type="Gelf4Net.Layout.GelfLayout, Gelf4Net.Core">
	    <param name="AdditionalFields" value="application:liget,Level:%level" />
	    <param name="Facility" value="gelf4net" />
	    <param name="IncludeLocationInformation" value="true"/>
	  </layout>
  </appender>
  <root>
    <level value="${LIGET_LOG_LEVEL}" />
    <appender-ref ref="GelfUdpAppender"/>
  </root>
</log4net>
EOF
fi

###########################################################################
# Used as fix-uid-gid solution in docker, almost copied from:
# https://github.com/tomzo/docker-uid-gid-fix/blob/master/fix-uid-gid.sh
###########################################################################

# This is the directory we expect to be mounted as docker volume.
# From that directory we know uid and gid.
DIRECTORY="/data"
OWNER_USERNAME="liget"
OWNER_GROUPNAME="liget"

if [ ! -d "$DIRECTORY" ]; then
  echo "$DIRECTORY does not exist, expected to be mounted as docker volume"
  exit 1;
fi

ret=false
getent passwd $OWNER_USERNAME >/dev/null 2>&1 && ret=true

if ! $ret; then
    echo "User $OWNER_USERNAME does not exist"
    exit 1;
fi
ret=false
getent passwd $OWNER_GROUPNAME >/dev/null 2>&1 && ret=true
if ! $ret; then
    echo "Group $OWNER_GROUPNAME does not exist"
    exit 1;
fi

NEWUID=$(ls --numeric-uid-gid -d $DIRECTORY | awk '{ print $3 }')
NEWGID=$(ls --numeric-uid-gid -d $DIRECTORY | awk '{ print $4 }')
OLDUID=$(id -u liget)
OLDGID=$(id -g liget)

if [[ $NEWUID != $OLDUID && $NEWUID != 0 ]]; then
  usermod -u $NEWUID $OWNER_USERNAME
fi
if [[ $NEWGID != $OLDGID && $NEWGID != 0 ]]; then
  groupmod -g $NEWGID $OWNER_GROUPNAME
fi
# Might be needed if the image has files which should be owned by
# this user and group. When we know more about user and group, then
# this find might be at smaller scope.
# In this case, image has only <%= @user_home %> owned by 1000
# find <%= @user_home %> -user 1000 -exec chown -h $NEWUID {} \;
# find <%= @user_home %> -group 1000 -exec chgrp -h $NEWGID {} \;
chown $NEWUID:$NEWGID -R /etc/liget /home/liget

cat << EOF > /app/LiGet.App.runtimeconfig.json
{
  "runtimeOptions": {
    "tfm": "netcoreapp2.0",
    "framework": {
      "name": "Microsoft.NETCore.App",
      "version": "2.0.0"
    },
    "configProperties": {
      "System.GC.Concurrent": $LIGET_GC_CONCURRENT,
      "System.GC.Server": $LIGET_GC_SERVER,
      "System.Threading.ThreadPool.MinThreads": $LIGET_THREAD_POOL_MIN,
      "System.Threading.ThreadPool.MaxThreads": $LIGET_THREAD_POOL_MAX
    }
  }
}
EOF

cd /data
if [[ $NEWGID != 0 ]]; then
  exec sudo -u liget -E -H dotnet /app/LiGet.App.dll
else
  echo "WARNING: running liget as root"
  exec dotnet /app/LiGet.App.dll
fi
