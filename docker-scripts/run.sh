#!/bin/bash
set -e

#-> Comments with '->' can be deleted, if you want it.

#-> It's bad pratice upper case variables. It can accidentaly replace a system variable.
#-> CamelCase is the best choice in my opinion
#-> declare -x = exportable variables (subshells can recognise it)
#-> declare -r = readonly variable (I call it constant)
declare -xr Directory="/data/simple2"
declare -xr OwnerUserName="liget"
declare -xr OwnerGroupName="liget"
declare -x  ErrorMsg=""

# First deployment bootstrap, we might want to change permissions of mounted volumes
if [[ ! -f /data/ef.sqlite/sqlite.db ]]; then
  echo "Database does not exist yet. Setting up directory access"
  mkdir -p "$Directory" /data/ef.sqlite /cache/simple2
  chown -R liget:liget /data/ /cache/
fi

###########################################################################
# Used as fix-uid-gid solution in docker, almost copied from:
# https://github.com/tomzo/docker-uid-gid-fix/blob/master/fix-uid-gid.sh
###########################################################################

# This is the directory we expect to be mounted as docker volume.
# From that directory we know uid and gid.

#-> A fancy way to report an error
[[ ! -d "$Directory" ]] \
  && ErrorMsg="* $Directory does not exist, expected to be mounted as docker volume\n"

getent passwd $OwnerUserName >/dev/null 2>&1 \
  || ErrorMsg="${ErrorMsg}* User $OwnerUserName does not exist\n"

getent passwd $OwnerGroupName >/dev/null 2>&1 \
  || ErrorMsg="${ErrorMsg}* Group $OwnerGroupName does not exist\n"

if [[ -n "$ErrorMsg" ]]; then
  echo -e "$ErrorMsg"
  exit 1
fi

#-> < <(command) = process substitution:
# https://www.gnu.org/software/bash/manual/html_node/Process-Substitution.html
read s s NewUID NewGID s < <(ls --numeric-uid-gid -d $Directory)
declare -r OldUID=$(id -u liget)
declare -r OldGID=$(id -g liget)

[[ "$NewUID" != "$OldUID" && "$NewUID" != 0 ]] && usermod  -u "$NewUID" "$OwnerUserName"
[[ "$NewGID" != "$OldGID" && "$NewGID" != 0 ]] && groupmod -g "$NewGID" "$OwnerGroupName"
chown "$NewUID":"$NewGID" -R /home/liget

###########################################################################
# Start server
###########################################################################

/usr/bin/configure-liget

cd /app
#->I have no ideia why this '+x' are here. Because of it, this 'if' statement will be aways true
if [[ -n "${LIGET_IMPORT_ON_BOOT+x}" ]]; then 
  if [[ "$NewGID" != 0 ]]; then
    sudo -u liget -E -H dotnet /app/LiGet.dll import --path "${LIGET_IMPORT_ON_BOOT}"
  else
    echo "WARNING: running liget as root"
    dotnet /app/LiGet.dll import --path "${LIGET_IMPORT_ON_BOOT}"
  fi
fi

if [[ "$NewGID" != 0 ]]; then
  exec sudo -u liget -E -H dotnet /app/LiGet.dll
else
  echo "WARNING: running liget as root"
  exec dotnet /app/LiGet.dll
fi
