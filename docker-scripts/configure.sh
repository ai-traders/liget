#!/bin/bash -e

declare -r config_json=/app/appsettings.json
declare -r runtime_config=/app/LiGet.runtimeconfig.json

if [ "${LIGET_SKIP_APPCONFIG_GEN}" == "true" ]; then
  echo "LIGET_SKIP_APPCONFIG_GEN is set. Will not generate $config_json"
else
  jq --arg cfg "${LIGET_API_KEY_HASH}" '. | .ApiKeyHash = $cfg' "$config_json" | sponge "$config_json"
  # .Database
  jq --arg cfg "${LIGET_EF_RUN_MIGRATIONS}" '. | .Database.RunMigrations = ($cfg == "true")' "$config_json" | sponge "$config_json"
  jq --arg cfg "${LIGET_DB_TYPE}" '. | .Database.Type = $cfg' "$config_json" | sponge "$config_json"
  jq --arg cfg "${LIGET_DB_CONNECTION_STRING}" '. | .Database.ConnectionString = $cfg' "$config_json" | sponge "$config_json"
  if [ "${LIGET_STORAGE_BACKEND}" == "simple2" ]; then
    jq '.| .Storage .Type = "FileSystem"' "$config_json" | sponge "$config_json"
    jq --arg cfg "${LIGET_SIMPLE2_ROOT_PATH}" '. | .Storage.Path = $cfg' "$config_json" | sponge "$config_json"
  fi
  # .Search
  jq --arg cfg "${LIGET_SEARCH_PROVIDER}" '. | .Search.Type = $cfg' "$config_json" | sponge "$config_json"

  # .Cache
  jq --arg cfg "${LIGET_CACHE_ENABLED}" '. | .Cache.Enabled = ($cfg == "true")' "$config_json" | sponge "$config_json"
  jq --arg cfg "${LIGET_CACHE_PROXY_SOURCE_INDEX}" '. | .Cache.UpstreamIndex = $cfg' "$config_json" | sponge "$config_json"
  if [ "${LIGET_NUPKG_CACHE_BACKEND}" == "simple2" ]; then
    jq --arg cfg "${LIGET_NUPKG_CACHE_SIMPLE2_ROOT_PATH}" '. | .Cache.PackagesPath = $cfg' "$config_json" | sponge "$config_json"
  fi

  jq --arg cfg "${LIGET_BAGET_COMPAT_ENABLED}" '. | .BaGetCompat.Enabled = ($cfg == "true")' "$config_json" | sponge "$config_json"

  if [ "${LIGET_LOG_BACKEND}" == "console" ]; then
    jq --arg cfg "${LIGET_LOG_LEVEL}" '. | .Logging.Console.LogLevel.Default = $cfg' "$config_json" | sponge "$config_json"
  elif [ "${LIGET_LOG_BACKEND}" == "gelf" ]; then
    if [ -z "${LIGET_LOG_GELF_HOST}" ]; then
      echo "LIGET_LOG_GELF_HOST must be specified when logging backend is gelf"
      exit 1
    fi
    jq --arg cfg "${LIGET_LOG_LEVEL}" '. | .Logging.GELF.LogLevel.Default = $cfg' "$config_json" | sponge "$config_json"
    jq --arg cfg "${LIGET_LOG_GELF_HOST}" '. | .Graylog.Host = $cfg' "$config_json" | sponge "$config_json"
    jq --arg cfg "${LIGET_LOG_GELF_SOURCE}" '. | .Graylog.LogSource = $cfg' "$config_json" | sponge "$config_json"
    jq --arg cfg "${LIGET_LOG_GELF_PORT}" '. | .Graylog.Port = ($cfg | tonumber)' "$config_json" | sponge "$config_json"
    if [ -n "${LIGET_LOG_GELF_ENVIRONMENT}" ]; then
      jq --arg cfg "${LIGET_LOG_GELF_ENVIRONMENT}" '. | .Graylog.AdditionalFields.environment = $cfg' "$config_json" | sponge "$config_json"
    fi
  else
    echo "Unknown logging backend: ${LIGET_LOG_BACKEND}"
  fi

  echo "LiGet configuration generated:"
  cat "$config_json"
fi

if [ "${LIGET_SKIP_RUNTIMECONFIG_GEN}" == "true" ]; then
  echo "LIGET_SKIP_RUNTIMECONFIG_GEN is set. Will not generate $runtime_config"
else
  jq --arg cfg "${LIGET_GC_CONCURRENT}" '. | .configProperties."System.GC.Concurrent" = ($cfg == "true")' "$runtime_config" | sponge "$runtime_config"
  jq --arg cfg "${LIGET_GC_SERVER}" '. | .configProperties."System.GC.Server" = ($cfg == "true")' "$runtime_config" | sponge "$runtime_config"
  jq --arg cfg "${LIGET_THREAD_POOL_MIN}" '. | .configProperties."System.Threading.ThreadPool.MinThreads" = ($cfg | tonumber)' "$runtime_config" | sponge "$runtime_config"
  jq --arg cfg "${LIGET_THREAD_POOL_MAX}" '. | .configProperties."System.Threading.ThreadPool.MaxThreads" = ($cfg | tonumber)' "$runtime_config" | sponge "$runtime_config"

  echo "LiGet runtime configuration generated:"
  cat "$runtime_config"
fi
