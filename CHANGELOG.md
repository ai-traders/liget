### 1.0.3 (2019-Apr-27)

Updates in build system only:
 * move from IDE to Dojo
 * use ops scripts from kudulab

Changed default log level to `Warning`.

### 1.0.2 (2019-Mar-15)

 - added option to change default listen port

### 1.0.1 (2019-Feb-13)

 - fix returning metadata of packages with no dependencies \#8
 - remove not supported cache endpoints from index \#9

### 1.0.0 (2018-Nov-30)

Merge BaGet fork into LiGet.
 * Adds V3 implementation
 * Added simple SPA UI
 * Now using sqlite for indexing private packages
 * Provides easy migration from pre-1.0 and baget

### 0.3.0 (2018-Nov-29) - BaGet fork

Cherry-pick of upstream changes
 * improve database search
 * package deletion service with different behaviors
 * refactoring of storage services and tests

Fix not returning unlisted packages in caching proxy.

### 0.2.1 (2018-Oct-22) - BaGet fork

 * Fixed missing dependencies in V3 endpoints \#12

### 0.2.0 (2018-Oct-11) - BaGet fork

 * Added V2 implementation from LiGet
 * Added compatibility mode with LiGet to keep the same endpoints
 * V2 includes dependencies in package query results
 * Switch production base image to slim stretch
 * added importer to complete migration from LiGet
 * fix/adjust for deployments with root-owned volumes

### 0.1.0 (2018-Oct-09) - BaGet fork

First release with a changelog.
 - added unit, integration tests and e2e tests with paket and nuget cli.
 - added release cycle and testing of docker image using continuous delivery practices.
 - implements read-through cache, which [does not work upstream](https://github.com/loic-sharma/BaGet/issues/93)
 - uses paket and FAKE for build system.
 - uses [Carter](https://github.com/CarterCommunity/Carter) for routing rather than bare Asp routing.
 - adds ability to log to graylog
 - builds SPA as part of pipeline
 - fixes handling package upload by [older clients and paket](https://github.com/loic-sharma/BaGet/issues/106)
 - added flag to run database migrations only when enabled

### 0.2.2 (2018-Jan-11) - LiGet

 * fix \#1 - do not cache catalog pages on disk

### 0.2.1 (2017-Dec-15) - LiGet

 * minor fix to not save scan date on failure
 * fix forwarding requests when response is not successfull \#12154
 * fix cache never invalidating due to case mismatch \#11699

### 0.2.0 (2017-Nov-02) - LiGet

 * Add caching proxy with v3 API, including
   - DBreeze backend
   - Periodic cache invalidation based on upstream changes
 * Adds stress tests
 * Tweaked kestrel options to get better performance

### 0.1.2 (2017-Oct-31) - LiGet

 * cleanup unused code and tests
 * add paket end case with version constraint
 * client usage readme

### 0.1.1 (2017-Oct-30) - LiGet

Fix release scripts

### 0.1.0 (2017-Oct-30) - LiGet

Initial release.
