load '/opt/bats-support/load.bash'
load '/opt/bats-assert/load.bash'

@test "push log4net" {
  run /bin/bash -c "cd e2e/input/log4net.2.0.8 && nuget push log4net.2.0.8.nupkg -src http://liget:9011/api/v2"
  assert_output --partial "http://liget:9011/api/v2"
  assert_equal "$status" 0
}

@test "nuget install exact package version" {
  run /bin/bash -c "cd nuget && nuget install log4net -Version 2.0.8 -DisableParallelProcessing -NoCache -Source http://liget:9011/api/v2"
  assert_output --partial "http://liget:9011/api/v2"
  assert_equal "$status" 0
}

@test "paket install exact package version" {
  run /bin/bash -c "cd paket && paket install"
  assert_output --partial "http://liget:9011/api/v2"
  assert_equal "$status" 0
}

@test "dotnet restore exact package version" {
  run /bin/bash -c "cd dotnet && dotnet restore"
  assert_output --partial "http://liget:9011/api/v2"
  assert_equal "$status" 0
}
