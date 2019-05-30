# BrainTeaser.CreateDelegate
A little brain teaser for creating a delegate from a System.Type

There is an empty `CreateEmptyDelegate` method that needs to be implemented. It simply needs to create an empty delegate with the following behavior (all of these cases should be covered in the existing unit tests):
- All incoming parameters for the delegate should be ignored
- If the delegate has a return type of `Task` it should return `Task.CompletedTask`
- If the delegate has a return type of `Task<T>` it should return `Task.CompletedTask(default(T))`
- If the delegate has a non-void return type, it should return `default(t)`
- If the delegate has any out parameters, it should set them to their default values

There is a _solution_ branch in this repo if you would like to see a potential solution. PRs and alternate solutions accepted as well.
