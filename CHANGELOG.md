### 0.2.2 (2018-Jan-11)

 * fix \#1 - do not cache catalog pages on disk

### 0.2.1 (2017-Dec-15)

 * minor fix to not save scan date on failure
 * fix forwarding requests when response is not successfull \#12154
 * fix cache never invalidating due to case mismatch \#11699

### 0.2.0 (2017-Nov-02)

 * Add caching proxy with v3 API, including
   - DBreeze backend
   - Periodic cache invalidation based on upstream changes
 * Adds stress tests
 * Tweaked kestrel options to get better performance

### 0.1.2 (2017-Oct-31)

 * cleanup unused code and tests
 * add paket end case with version constraint
 * client usage readme

### 0.1.1 (2017-Oct-30)

Fix release scripts

### 0.1.0 (2017-Oct-30)

Initial release.
