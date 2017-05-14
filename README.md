# stormancer-Monitoring

Tools used to test & monitor Stormancer applications.

## Stormancer.Monitoring.SmokeTest
Smallweight .NET Stormancer client application that can be used to connect, run one or several RPC on an application,
then return CSV results

## Stormancer.Monitoring.SD.Plugin
Server density plugin which uses Smoketest to ensure one or several Stormancer applications are running.

The plugin retrieves outputs returned by Smoketest and send them as custom data to the SD agent.

## Stormancer.Loadtesting
A loadtesting frontend for Stormancer apps which orchestrates Smoketest clients.
