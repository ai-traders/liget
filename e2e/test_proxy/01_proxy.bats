load '/opt/bats-support/load.bash'
load '/opt/bats-assert/load.bash'

@test "nuget install latest log4net" {
  run /bin/bash -c "cd nuget && nuget install log4net -DisableParallelProcessing -NoCache -Source http://liget:9011/api/cache/v3/index.json"
  assert_output --partial "OK http://liget:9011/api/cache/v3/registration3-gz-semver2/log4net/index.json"
  assert_equal "$status" 0
}

@test "paket install latest log4net version" {
  run /bin/bash -c "cd paket && mono /ide/work/.paket/paket.exe install"
  refute_output --partial 'Could not download'
  refute_output --partial 'went wrong'
  assert_equal "$status" 0
  assert [ -e 'paket/packages/log4net' ]
}

@test "paket install with constraint (<= 2.0.8)" {
  run /bin/bash -c "cd paket-constraint && mono /ide/work/.paket/paket.exe install"
  refute_output --partial 'Could not download'
  refute_output --partial 'went wrong'
  assert_equal "$status" 0
  assert [ -e 'paket-constraint/packages/log4net/log4net.2.0.8.nupkg' ]
}
