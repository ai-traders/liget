load '/opt/bats-support/load.bash'
load '/opt/bats-assert/load.bash'

@test "push private package liget-two v1.0.0" {
  run /bin/bash -c "cd ../input/liget-two/bin/Debug/ && dotnet nuget push liget-two.1.0.0.nupkg --source http://liget:9011/api/v2"
  assert_output --partial "http://liget:9011/api/v2"
  assert_equal "$status" 0
}

@test "nuget install latest package version (1.0.0)" {
  run /bin/bash -c "cd nuget && nuget install liget-two -DisableParallelProcessing -NoCache -Source http://liget:9011/api/v2"
  assert_output --partial "http://liget:9011/api/v2"
  assert_equal "$status" 0
  assert [ -e 'nuget/liget-two.1.0.0' ]
}

@test "paket update latest package version (1.0.0)" {
  run /bin/bash -c "cd paket && mono /ide/work/.paket/paket.exe update"
  refute_output --partial 'Could not download'
  refute_output --partial 'went wrong'
  assert_equal "$status" 0
  assert [ -e 'paket/packages/liget-two/liget-two.1.0.0.nupkg' ]
}

# Publish newer version
@test "push private package liget-two v2.1.0" {
  run /bin/bash -c "cd ../input/liget-two/bin/Debug/ && dotnet nuget push liget-two.2.1.0.nupkg --source http://liget:9011/api/v2"
  assert_output --partial "http://liget:9011/api/v2"
  assert_equal "$status" 0
}

@test "nuget install latest package version (2.1.0)" {
  run /bin/bash -c "cd nuget && nuget install liget-two -DisableParallelProcessing -NoCache -Source http://liget:9011/api/v2"
  refute_output --partial 'Could not download'
  refute_output --partial 'went wrong'
  assert_output --partial "http://liget:9011/api/v2"
  assert_equal "$status" 0
  assert [ -e 'nuget/liget-two.2.1.0' ]
}

@test "paket update latest package version (2.1.0)" {
  run /bin/bash -c "cd paket && mono /ide/work/.paket/paket.exe update"
  refute_output --partial 'Could not download'
  refute_output --partial 'went wrong'
  assert_equal "$status" 0
  assert [ -e 'paket/packages/liget-two/liget-two.2.1.0.nupkg' ]
}

@test "paket install with constraint (< 2.0.0)" {
  run /bin/bash -c "cd paket-constraint && mono /ide/work/.paket/paket.exe install"
  refute_output --partial 'Could not download'
  refute_output --partial 'went wrong'
  assert_equal "$status" 0
  assert [ -e 'paket-constraint/packages/liget-two/liget-two.1.0.0.nupkg' ]
}
