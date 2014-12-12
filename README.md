AttributeCaching
================

A library enabling caching using declarative AOP approach (using attributes).

The AttributeCaching library is of read-through type. With a read-through model, the cache detects the missing item and calls the provider to perform the data load. The item is then seamlessly returned to the cache client. Whereas in the cache-aside programming model, the caller is responsible for then loading the data from a backend store and then putting that data in the cache.

Usage examples: http://blog.mshmelev.com/2014/12/attribute-caching.html
