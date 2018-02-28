# Dapper.SqlExtensions
:mega: **Dapper.Contrib already does that!** :confused: 

You might say so, but Dapper.Contrib does not use the same annotations as Entity framework does, 
that means that if you do a Scaffold with the -D parameter (Which makes it scaffold with data annotations) 
you won't be able to use your model with the contrib project.

# Usage
This lib is pretty streight forward, all you have to do is create a ```new SqlObject(myObject);``` and use its methods.
