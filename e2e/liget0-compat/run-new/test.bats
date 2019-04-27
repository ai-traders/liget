load '/opt/bats-support/load.bash'
load '/opt/bats-assert/load.bash'

# liget-two was pushed previously in older liget version before migration

@test "nuget install latest package version (1.0.0) via V3" {
  run /bin/bash -c "cd nuget && nuget install liget-two -DisableParallelProcessing -NoCache -DirectDownload -Source http://nuget:9011/api/v3/index.json"
  assert_output --partial "http://nuget:9011/api/v3/index.json"
  assert_equal "$status" 0
  assert [ -e 'nuget/liget-two.1.0.0' ]
}

@test "nuget install latest package version (1.0.0) via V2" {
  run /bin/bash -c "cd nuget2 && nuget install liget-two -DisableParallelProcessing -NoCache -DirectDownload -Source http://nuget:9011/api/v2"
  assert_output --partial "http://nuget:9011/api/v2"
  assert_equal "$status" 0
  assert [ -e 'nuget2/liget-two.1.0.0' ]
}

@test "paket restore packages locked with old liget" {
  run /bin/bash -c "cd paket && mono /dojo/work/e2e/.paket/paket.exe restore"
  refute_output --partial 'Could not download'
  refute_output --partial 'went wrong'
  assert_equal "$status" 0
}
