#!/bin/bash
set -e

DIRECTORY="/data/simple2"
OWNER_USERNAME="liget"
OWNER_GROUPNAME="liget"

# First deployment bootstrap, we might want to change permissions of mounted volumes
if [ ! -f /data/ef.sqlite/sqlite.db ]; then
  echo "Database does not exist yet. Setting up directory access"
  mkdir -p /data/simple2 /data/ef.sqlite /cache/simple2
  chown -R liget:liget /data/ /cache/
fi

###########################################################################
# Used as fix-uid-gid solution in docker, almost copied from:
# https://github.com/tomzo/docker-uid-gid-fix/blob/master/fix-uid-gid.sh
###########################################################################

# This is the directory we expect to be mounted as docker volume.
# From that directory we know uid and gid.

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
chown $NEWUID:$NEWGID -R /home/liget

###########################################################################
# Start server
###########################################################################

/usr/bin/configure-liget

cd /app
if [ ! -z ${LIGET_IMPORT_ON_BOOT+x} ]; then
  if [[ $NEWGID != 0 ]]; then
    sudo -u liget -E -H dotnet /app/LiGet.dll import --path ${LIGET_IMPORT_ON_BOOT}
  else
    echo "WARNING: running liget as root"
    dotnet /app/LiGet.dll import --path ${LIGET_IMPORT_ON_BOOT}
  fi
fi

if [[ $NEWGID != 0 ]]; then
  exec sudo -u liget -E -H dotnet /app/LiGet.dll
else
  echo "WARNING: running liget as root"
  exec dotnet /app/LiGet.dll
fi
