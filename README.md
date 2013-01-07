Database-Schema-Reader
======================

Database Schema Reader

This is a fork of http://dbschemareader.codeplex.com to add customizations for Fluent nHibernate code generation.

On some legacy database, features such as "pluralization" need to be turned off (e.g. database with both _drink_ and _drinks_ tables)
Also need suppor to filter out unwanted tables so that the entire database doesn't need to be generated (e.g. capability to filter out backup tables, temporary tables, etc.)
