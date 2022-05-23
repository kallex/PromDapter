PromDapter = Prometheus Adapter
-------------------------------

Currently in Pre-Release/Beta stage
- Lightly documented
- Source available, but not completely buildable due to HWiNFO Provider being private NuGet for the time being
- UPDATE ^: HWinFO Provider also now Open Sourced: https://github.com/kallex/PromDapterHWiNFO/



Pre-requisites:
- HWiNFO running with Shared Memory enabled and Sensor-mode (window) opened, can be hidden afterwards

v0.9.xx-beta
Setup:
- Windows 64-bit supported only (should be trivial to make 32-bit, open an issue if needed)
- Sets up LocalSystem - running Service called "Prometheus Adapter" (PromDapterSvc)
- Service is serving on port 10445 (need to open it in firewall for TCP protocol)
- Setup is supposed to not overwrite existing 

- Alpha/beta stage: Windows Management Instrumentation (WMI) support added

URLs:
http://localhost:10445/metrics
- Serves Prometheus-formed metrics

http://localhost:10445/metrics/help
- Lists # HELP tagged parts of metrics for (easier?) review of what's available

http://localhost:10445/metrics/reset
- Resets internal caches (= reloads "C:\ProgramData\PromDapter\Prometheusmapping.yaml" on next request)

JSON support (does not support WMI metrics currently):
http://localhost:10445/metrics/json
http://localhost:10445/metrics/json?option=flattenMeta


Configuration/Metric definition:
- Modify C:\ProgramData\PromDapter\Prometheusmapping.yaml (save your copy safely elsewhere and copy over + reset with url above)
(The installation version of the file is also available in "C:\Program Files\PromDapter")

TODO (next):
- Configurable server port (currently fixed 10445)
- Configurable metric prefix (currently fixed hwi_ & wmi_)

TODO (needs to be done):
- Finish remaining HWiNFO sensors (various voltage, fan sensor naming properly)
- Handle naming consolidation properly, ie. Ryzen 2xxx series Tctl needs right now be renamed in HWiNFO to match 1xxx and 3xxx series
