Database-Schema-Reader
======================

Database Schema Reader

This is a _fork_ of http://dbschemareader.codeplex.com to add customizations for Fluent nHibernate code generation.

On some legacy database, features such as "pluralization" need to be turned off (e.g. database with both _drink_ and _drinks_ tables)
Also need suppor to filter out unwanted tables so that the entire database doesn't need to be generated (e.g. capability to filter out backup tables, temporary tables, etc.)

Most of the changes here are specific to composite foreign keys.
These were needed for a very large legacy system.

With these changes, we are able to generate a full, working Fluent nHibernate mapping for 1500+ tables with their proper relationships.