# Leverage the coveralls gem
require 'coveralls'
# Include this gem
require 'coveralls-cobertura'
# Coveralls endpoint that we want to send coverage data to
JOBS_ENDPOINT = 'jobs'
# Assumes you already have a payload
existing_source_files = payload[:source_files]
# Cobertura XML file
filename = 'outputCobertura.xml'
# Create a Converter instance
converter = Coveralls::Cobertura::Converter.new(filename)
# Convert to Coveralls
cobertura_source_files = converter.convert
# Add in the Cobertura generated source files
payload[:source_files] = existing_source_files + cobertura_source_files
Coveralls::API.post_json(JOBS_ENDPOINT, payload)