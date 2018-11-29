load '/opt/bats-support/load.bash'
load '/opt/bats-assert/load.bash'

@test "push private package liget-two v1.0.0" {
  run /bin/bash -c "cd ../../input/liget-two/bin/Debug/ && dotnet nuget push liget-two.1.0.0.nupkg --source http://nuget:9011/api/v2"
  assert_output --partial "http://nuget:9011/api/v2"
  assert_equal "$status" 0
}
