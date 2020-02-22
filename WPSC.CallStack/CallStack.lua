do
    CallStack = {}

    local stack = {}

    function CallStack:Register(name, file, line)
        local entry = {
            name = name,
            file = file,
            line = line,
        }
        table.insert(stack, entry)
    end

    function CallStack:ToString()
        local ret = ""
        local count = #stack
        for i=count,1,-1 do
            local id = count - i + 1
            local entry = stack[i]
            if #ret ~= 0 then
                ret = ret .. "\n"
            end
            ret = ret .. "[" .. id .. "] " .. entry.name .. " in <" .. entry.file .. ":" .. entry,line .. ">"
        end
    end
    
    local oldError = error
    
    function error(msg, level)
        msg = msg .. "\n----- at -----\n" .. CallStack:ToString()
        error(msg, level)
    end
end
