load '/opt/bats-support/load.bash'
load '/opt/bats-assert/load.bash'

@test "push log4net" {
  run /bin/bash -c "cd ../input/liget-test1/bin/Debug/ && nuget push liget-test1.1.0.0.nupkg -src http://liget:9011/api/v2"
  assert_output --partial "http://liget:9011/api/v2"
  assert_equal "$status" 0
}

@test "nuget install exact package version" {
  run /bin/bash -c "cd nuget && nuget install liget-test1 -Version 1.0.0 -DisableParallelProcessing -NoCache -Source http://liget:9011/api/v2"
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
  assert_output --partial "liget-test1"
  assert_equal "$status" 0
}
