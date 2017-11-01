load '/opt/bats-support/load.bash'
load '/opt/bats-assert/load.bash'

@test "paket update to download a bunch of packages" {
  run /bin/bash -c "cd paket && paket update"
  assert_equal "$status" 0
}

@test "nuget push a bunch of packages" {
  run /bin/bash -c "cd paket && ./push_all.sh"
  assert_equal "$status" 0
}
