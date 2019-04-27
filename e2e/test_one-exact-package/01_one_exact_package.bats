load '/opt/bats-support/load.bash'
load '/opt/bats-assert/load.bash'

@test "push package example3 with no dependencies" {
  run /bin/bash -c "cd ../input/example3 && dotnet nuget push example3.1.0.0.nupkg --source http://liget:9011/api/v3/index.json --api-key NUGET-SERVER-API-KEY"
  assert_output --partial "Your package was pushed"
  assert_equal "$status" 0
}

@test "nuget install package with no dependencies and exact version via V3" {
  run /bin/bash -c "cd nuget && nuget install example3 -Version 1.0.0 -DisableParallelProcessing -NoCache -Source http://liget:9011/api/v3/index.json"
  assert_output --partial "http://liget:9011/api/v3/index.json"
  assert_equal "$status" 0
}

@test "push private package liget-test1" {
  run /bin/bash -c "cd ../input/liget-test1/bin/Debug/ && dotnet nuget push liget-test1.1.0.0.nupkg --source http://liget:9011/api/v3/index.json --api-key NUGET-SERVER-API-KEY"
  assert_output --partial "Your package was pushed"
  assert_equal "$status" 0
}

@test "nuget install exact package version via V3" {
  run /bin/bash -c "cd nuget && nuget install liget-test1 -Version 1.0.0 -DisableParallelProcessing -NoCache -Source http://liget:9011/api/v3/index.json"
  assert_output --partial "http://liget:9011/api/v3/index.json"
  assert_equal "$status" 0
}

@test "nuget install exact package version via V2" {
  run /bin/bash -c "cd nuget2 && nuget install liget-test1 -Version 1.0.0 -DisableParallelProcessing -NoCache -Source http://liget:9011/api/v2"
  assert_output --partial "http://liget:9011/api/v2"
  assert_equal "$status" 0
}

@test "paket install pinned package version" {
  run /bin/bash -c "cd paket-pinned && mono /dojo/work/e2e/.paket/paket.exe install"
  assert_output --partial "Installing into projects"
  refute_output --partial 'Could not download'
  refute_output --partial 'went wrong'
  assert_equal "$status" 0
}

@test "paket restore pinned package version" {
  run /bin/bash -c "cd paket-locked && mono /dojo/work/e2e/.paket/paket.exe restore"
  refute_output --partial 'Could not download'
  refute_output --partial 'went wrong'
  assert_equal "$status" 0
}

@test "dotnet restore exact package version" {
  run /bin/bash -c "cd dotnet && dotnet restore --no-cache"
  assert_output --partial "Restore completed"
  assert_equal "$status" 0
}
